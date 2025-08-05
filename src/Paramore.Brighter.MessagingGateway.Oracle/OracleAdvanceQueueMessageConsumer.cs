using System;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Paramore.Brighter.MessagingGateway.Oracle;

/// <summary>
/// Provides message consumption capabilities for Oracle Advanced Queuing (AQ) system.
/// Implements synchronous and asynchronous message consumption patterns including
/// message receipt, acknowledgment, rejection, requeuing, and queue purging.
/// </summary>
/// <remarks>
/// <para>
/// This consumer handles Oracle AQ messages through Brighter's message processing model.
/// It supports both raw and structured payload types (OracleString/OracleBinary) and manages
/// Oracle-specific transaction handling for reliable message processing.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
///   <item>Transactional message handling with explicit commit/rollback</item>
///   <item>Support for persistent and buffered delivery modes</item>
///   <item>Configurable visibility modes (OnCommit/Immediate)</item>
///   <item>Message requeue capabilities with optional delays</item>
///   <item>Bulk queue purging via DBMS_AQADM</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="configuration">Database configuration containing connection details</param>
/// <param name="connectionProvider">Provider for Oracle database connections</param>
/// <param name="subscription">Configuration for queue subscription parameters</param>
public class OracleAdvanceQueueMessageConsumer(
    IAmARelationalDatabaseConfiguration configuration, 
    IAmARelationalDbConnectionProvider connectionProvider,
    OracleAdvanceQueueSubscription subscription) : IAmAMessageConsumerSync, IAmAMessageConsumerAsync
{
    private OracleAQQueue? _queue;
    private OracleTransaction? _transaction;
    
    /// <inheritdoc />
    public Task AcknowledgeAsync(Message message, CancellationToken cancellationToken = default)
    {
        Acknowledge(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Acknowledge(Message message)
    {
        try
        {
            var transaction = GetTransaction();
            transaction.Commit();
        }
        finally
        {
            Release();
        } 
    }
    
    /// <inheritdoc />
    public Task<bool> RejectAsync(Message message, CancellationToken cancellationToken = default) 
        => Task.FromResult(Reject(message));

    /// <inheritdoc />
    public bool Reject(Message message)
    {
        try
        {
            var transaction = GetTransaction();
            transaction.Rollback();
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            Release();
        }
    }
    
    /// <inheritdoc />
    public async Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        var connection = (OracleConnection)await connectionProvider.GetConnectionAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "DBMS_AQADM.PURGE_QUEUE_TABLE";
        
        // Add the 'queue_table' parameter.
        var queueTableParam = new OracleParameter("queue_table", OracleDbType.Varchar2, configuration.QueueStoreTable, System.Data.ParameterDirection.Input);
        command.Parameters.Add(queueTableParam);
        
        // Add the 'purge_condition' parameter.
        var purgeConditionParam = new OracleParameter("purge_condition", OracleDbType.Varchar2, null, System.Data.ParameterDirection.Input);
        command.Parameters.Add(purgeConditionParam);
        
        // Add the 'block' parameter.
        var blockParam = new OracleParameter("block", OracleDbType.Boolean, true, System.Data.ParameterDirection.Input);
        command.Parameters.Add(blockParam);

        // Execute the command. ExecuteNonQuery is used for commands that don't return data.
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Purge()
    {
        var connection = (OracleConnection)connectionProvider.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DBMS_AQADM.PURGE_QUEUE_TABLE";
        
        // Add the 'queue_table' parameter.
        var queueTableParam = new OracleParameter("queue_table", OracleDbType.Varchar2, configuration.QueueStoreTable, System.Data.ParameterDirection.Input);
        command.Parameters.Add(queueTableParam);
        
        // Add the 'purge_condition' parameter.
        var purgeConditionParam = new OracleParameter("purge_condition", OracleDbType.Varchar2, null, System.Data.ParameterDirection.Input);
        command.Parameters.Add(purgeConditionParam);
        
        // Add the 'block' parameter.
        var blockParam = new OracleParameter("block", OracleDbType.Boolean, true, System.Data.ParameterDirection.Input);
        command.Parameters.Add(blockParam);

        // Execute the command. ExecuteNonQuery is used for commands that don't return data.
        command.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public Task<Message[]> ReceiveAsync(TimeSpan? timeOut = null, CancellationToken cancellationToken = default) 
        => Task.FromResult(Receive(timeOut));

    /// <inheritdoc />
    public Message[] Receive(TimeSpan? timeOut = null)
    {
        var transaction = GetTransaction();
        try
        {
            var queue = GetOrCreateQueue();
            return ToBrighterMessage(queue.Dequeue());
        }
        catch (Exception)
        {
            transaction.Rollback();
            Release();
            throw;
        }
    }
    
    /// <inheritdoc />
    public Task<bool> RequeueAsync(Message message, TimeSpan? delay = null,
        CancellationToken cancellationToken = default) 
        => Task.FromResult(Requeue(message));

    /// <inheritdoc />
    public bool Requeue(Message message, TimeSpan? delay = null)
    {
        if (!message.Header.Bag.TryGetValue("ReceiptHandler", out var tmp)
            || tmp is not OracleAQMessage oracleAqMessage)
        {
            return false;
        }

        try
        {
            var transaction = GetTransaction();
            var queue = GetOrCreateQueue();
            queue.Enqueue(oracleAqMessage);
            transaction.Commit();
            return true;
        }
        finally
        {
            Release();
        }
    }

    private Message[] ToBrighterMessage(OracleAQMessage? oracleAqMessage)
    {
        if (oracleAqMessage == null)
        {
            return [new Message()];
        }

        var header = new MessageHeader
        {
            MessageId = Id.Create(subscription.Encoding.GetString(oracleAqMessage.MessageId)),
            CorrelationId = oracleAqMessage.Correlation,
            HandledCount = oracleAqMessage.DequeueAttempts,
        };
            
        header.Bag.Add("ReceiptHandler", oracleAqMessage);
        MessageBody body;
        if (oracleAqMessage.Payload is OracleString oracleString)
        {
            body = new MessageBody(oracleString.Value);
        }
        else if(oracleAqMessage.Payload is OracleBinary oracleBinary)
        {
            body = new MessageBody(oracleBinary.Value); 
        }
        else
        {
            body = new MessageBody("");
            header.Bag.Add(HeaderNames.Payload, oracleAqMessage);
        }

        return [new Message(header, body)];
    }

    private OracleAQQueue GetOrCreateQueue()
    {
        if (_queue != null)
        {
            return _queue;
        }
        
        var connection = (OracleConnection)connectionProvider.GetConnection();
        _queue = new OracleAQQueue(configuration.QueueStoreTable, connection)
        {
            MessageType = subscription.MessageType,
            DequeueOptions = new OracleAQDequeueOptions
            {
                ConsumerName = subscription.ConsumerName,
                DequeueMode = subscription.DequeueMode,
                Wait = (int)subscription.TimeOut.TotalMilliseconds,
                Visibility = subscription.Visibility,
            }
        };
        
        return _queue;
    }

    private OracleTransaction GetTransaction()
    {
        if (_transaction != null)
        {
            return _transaction;
        }
        
        var connection = (OracleConnection)connectionProvider.GetConnection();
        _transaction = connection.BeginTransaction();
        return _transaction;
    }
    
    private void Release()
    {
        _transaction = null;
        _queue = null;
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return new ValueTask();
    }
}
