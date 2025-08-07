using System;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Paramore.Brighter.MessagingGateway.Oracle;

public class OracleAdvanceQueuePullMessageConsumer(string connectionString, 
    OracleAdvanceQueueSubscription subscription,
    Encoding encoding) 
    : IAmAMessageConsumerSync, IAmAMessageConsumerAsync
{
    private OracleConnection? _connection;
    private OracleTransaction? _transaction;
    private OracleAQQueue? _queue;

    /// <inheritdoc />
#if NETFRAMEWORK
    public Task AcknowledgeAsync(Message message, CancellationToken cancellationToken = default)
    {
        Acknowledge(message);
        return Task.CompletedTask;
    }
#else
    public async Task AcknowledgeAsync(Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction == null)
            {
                return;
            }
            
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await ReleaseAsync();
        } 
    }
#endif
    

    /// <inheritdoc />
    public void Acknowledge(Message message)
    {
        try
        {
            _transaction?.Commit();
        }
        finally
        {
            Release();
        } 
    }

    /// <inheritdoc />
#if NETFRAMEWORK
    public Task<bool> RejectAsync(Message message, CancellationToken cancellationToken = default) 
        => Task.FromResult(Reject(message));
#else
    public async Task<bool> RejectAsync(Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            await ReleaseAsync();
        }
    }
#endif

    /// <inheritdoc />
    public bool Reject(Message message)
    {
        try
        {
            _transaction?.Rollback();
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
#if NETFRAMEWORK
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction =  connection.BeginTransaction();
        using var command = connection.CreateCommand();
#else
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        
        await using var transaction =  await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
#endif
        
        command.CommandText = "DBMS_AQADM.PURGE_QUEUE_TABLE";
        command.CommandType = CommandType.StoredProcedure;
        
        // Add the 'queue_table' parameter.
        var queueTableParam = new OracleParameter("queue_table", OracleDbType.Varchar2, subscription.ChannelName, ParameterDirection.Input);
        command.Parameters.Add(queueTableParam);
        
        // Add the 'purge_condition' parameter.
        var purgeConditionParam = new OracleParameter("purge_condition", OracleDbType.Varchar2, null, ParameterDirection.Input);
        command.Parameters.Add(purgeConditionParam);
        
        // Add the 'block' parameter.
        var blockParam = new OracleParameter("block", OracleDbType.Boolean, true, ParameterDirection.Input);
        command.Parameters.Add(blockParam);

        // Execute the command. ExecuteNonQuery is used for commands that don't return data.
        await command.ExecuteNonQueryAsync(cancellationToken);
        
#if NETFRAMEWORK
        transaction.Commit();
#else
        await transaction.CommitAsync(cancellationToken);
#endif
    }

    /// <inheritdoc />
    public void Purge()
    {
        using var connection = new OracleConnection(connectionString);
        connection.Open();
        
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.CommandText = "DBMS_AQADM.PURGE_QUEUE_TABLE";
        command.CommandType = CommandType.StoredProcedure;
        
        // Add the 'queue_table' parameter.
        var queueTableParam = new OracleParameter("queue_table", OracleDbType.Varchar2, subscription.ChannelName, ParameterDirection.Input);
        command.Parameters.Add(queueTableParam);
        
        // Add the 'purge_condition' parameter.
        var purgeConditionParam = new OracleParameter("purge_condition", OracleDbType.Varchar2, null, ParameterDirection.Input);
        command.Parameters.Add(purgeConditionParam);
        
        // Add the 'block' parameter.
        var blockParam = new OracleParameter("block", OracleDbType.Boolean, true, ParameterDirection.Input);
        command.Parameters.Add(blockParam);

        // Execute the command. ExecuteNonQuery is used for commands that don't return data.
        command.ExecuteNonQuery();
        
        transaction.Commit();
    }

    /// <inheritdoc />
    public async Task<Message[]> ReceiveAsync(TimeSpan? timeOut = null, CancellationToken cancellationToken = default)
    {
#if NETFRAMEWORK
        Release();
#else
        await ReleaseAsync();
#endif

        _connection = new OracleConnection(connectionString);
        await _connection.OpenAsync(cancellationToken);
        
#if NETFRAMEWORK
        _transaction = _connection.BeginTransaction();
#else
        var transaction = await _connection.BeginTransactionAsync(cancellationToken);
        _transaction = (OracleTransaction)transaction;
#endif
        _queue = new OracleAQQueue(subscription.ChannelName, _connection)
        {
            UdtTypeName = subscription.UdtTypeName,
            MessageType = subscription.MessageType,
            DequeueOptions = new OracleAQDequeueOptions
            {
                ConsumerName = subscription.ConsumerName,
                Correlation = subscription.Correlation,
                DeliveryMode = subscription.DeliveryMode,
                DequeueMode = subscription.DequeueMode,
                MessageId = new byte[subscription.MessageIdLength],
                NavigationMode = subscription.NavigationMode,
                ProviderSpecificType = subscription.ProviderSpecificType,
                Visibility = subscription.Visibility,
                Wait = (int)subscription.TimeOut.TotalMilliseconds
            }
        };
        
        try
        {
            return ToBrighterMessage(_queue.Dequeue());
        }
        catch (Exception)
        {
#if NETFRAMEWORK
            _transaction.Rollback();
            Release();
#else
            await _transaction.RollbackAsync(cancellationToken);
            await ReleaseAsync();
#endif
            throw;
        }
    }

    /// <inheritdoc />
    public Message[] Receive(TimeSpan? timeOut = null)
    {
        Release();

        _connection = new OracleConnection(connectionString);
        _connection.Open();
        
        _transaction = _connection.BeginTransaction();
        
        _queue = new OracleAQQueue(subscription.ChannelName, _connection)
        {
            UdtTypeName = subscription.UdtTypeName,
            MessageType = subscription.MessageType,
            DequeueOptions = new OracleAQDequeueOptions
            {
                ConsumerName = subscription.ConsumerName,
                Correlation = subscription.Correlation,
                DeliveryMode = subscription.DeliveryMode,
                DequeueMode = subscription.DequeueMode,
                MessageId = new byte[subscription.MessageIdLength],
                NavigationMode = subscription.NavigationMode,
                ProviderSpecificType = subscription.ProviderSpecificType,
                Visibility = subscription.Visibility,
                Wait = (int)subscription.TimeOut.TotalMilliseconds
            }
        };
        
        try
        {
            return ToBrighterMessage(_queue.Dequeue());
        }
        catch (Exception)
        {
            _transaction?.Rollback();
            Release();
            throw;
        }
    }

    /// <inheritdoc />
#if NETFRAMEWORK
    public Task<bool> RequeueAsync(Message message, TimeSpan? delay = null,CancellationToken cancellationToken = default) 
        => Task.FromResult(Requeue(message));
#else
    public async Task<bool> RequeueAsync(Message message, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        if (!message.Header.Bag.TryGetValue("ReceiptHandler", out var tmp) || tmp is not OracleAQMessage oracleAqMessage)
        {
            return false;
        }
        
        try
        {
            if (_transaction != null && _queue != null)
            {
                var enqueue = new OracleAQMessage
                {
                    Correlation = oracleAqMessage.Correlation,
                    Delay = (int)delay.GetValueOrDefault().TotalSeconds,
                    ExceptionQueue = oracleAqMessage.ExceptionQueue,
                    Expiration = oracleAqMessage.Expiration,
                    Payload = oracleAqMessage.Payload,
                    PreviousQueueMessageId = oracleAqMessage.MessageId,
                    Priority = oracleAqMessage.Priority,
                    Recipients = oracleAqMessage.Recipients,
                    SenderId = oracleAqMessage.SenderId,
                    ShardNum = oracleAqMessage.ShardNum
                };

                _queue.Enqueue(enqueue);
                await _transaction.CommitAsync(cancellationToken);
            }
            
            return true;
        }
        finally
        {
            await ReleaseAsync();
        }
    }
#endif

    /// <inheritdoc />
    public bool Requeue(Message message, TimeSpan? delay = null)
    {
        if (!message.Header.Bag.TryGetValue(HeaderNames.ReceiptHandler, out object? tmp)
            || tmp is not OracleAQMessage oracleAqMessage)
        {
            return false;
        }
        
        try
        {
            if (_transaction != null && _queue != null)
            {
                var enqueue = new OracleAQMessage
                {
                    Correlation = oracleAqMessage.Correlation,
                    Delay = (int)delay.GetValueOrDefault().TotalSeconds,
                    ExceptionQueue = oracleAqMessage.ExceptionQueue,
                    Expiration = oracleAqMessage.Expiration,
                    Payload = oracleAqMessage.Payload,
                    PreviousQueueMessageId = oracleAqMessage.MessageId,
                    Priority = oracleAqMessage.Priority,
                    Recipients = oracleAqMessage.Recipients,
                    SenderId = oracleAqMessage.SenderId,
                    ShardNum = oracleAqMessage.ShardNum
                };

                _queue.Enqueue(enqueue);
                _transaction.Commit();
            }
            
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
            MessageId = Id.Create(encoding.GetString(oracleAqMessage.MessageId)),
            MessageType = MessageType.MT_EVENT,
            TimeStamp = oracleAqMessage.EnqueueTime,
            HandledCount = oracleAqMessage.DequeueAttempts,
        };


        if (!string.IsNullOrEmpty(oracleAqMessage.Correlation))
        {
            header.PartitionKey = new PartitionKey(oracleAqMessage.Correlation);
        }
            
        header.Bag.Add(HeaderNames.ReceiptHandler, oracleAqMessage);
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
    
    private void Release()
    {
        if (_queue != null)
        {
            _queue.Dispose();
            _queue = null;
        }
        
        if (_transaction != null)
        {
            _transaction.Dispose();
            _transaction = null;
        }
        
        if (_connection != null)
        {
            _connection.Dispose();
            _connection = null;
        }
    }

#if NET8_0_OR_GREATER
    private async Task ReleaseAsync()
    {
        if (_queue != null)
        {
            _queue.Dispose();
            _queue = null;
        }
        
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
#endif
    
    /// <inheritdoc />
    public void Dispose() => Release();

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
#if NETFRAMEWORK
        Release();
        return new ValueTask();
#else
        return new ValueTask(ReleaseAsync());
#endif
    }
}
