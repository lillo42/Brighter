using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

internal static class QueueFactory
{
    public static void EnsureExists(string connectionString, 
        OnMissingChannel makeChannel, 
        QueueAttribute attribute)
    {
        if (makeChannel == OnMissingChannel.Assume)
        {
            return;
        }

        using var conn = new OracleConnection(connectionString);
        conn.Open();
        
        var exists = Exists(conn, attribute.Queue.Name);
        if (exists)
        {
            return;
        }
        
        if (makeChannel == OnMissingChannel.Validate)
        {
            throw new InvalidOperationException($"'{attribute.Queue.Name}' doesn't exist");
        }
        
        if (attribute.Queue.ExceptionQueue != null)
        {
            EnsureExists(connectionString, makeChannel, attribute.Queue.ExceptionQueue);
        }
            
        CreateQueueTable(conn, attribute.Table);
        CreateQueue(conn, attribute.Queue);
        StartQueue(conn, attribute.Queue);
    }

    private static bool Exists(OracleConnection connection, string queue)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM USER_QUEUES WHERE NAME = :queue_name";
        cmd.Parameters.Add(new OracleParameter("queue_name", queue));
        
        using var transaction = connection.BeginTransaction();
        cmd.Transaction = transaction;

        var res = cmd.ExecuteScalar();
        return res is > 0;
    }

    private static void CreateQueueTable(OracleConnection connection, QueueTable table)
    {
        using var command = connection.CreateCommand();
        SetCreateQueueTable(command, table);
        
        using var transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        command.ExecuteNonQuery();
        transaction.Commit();
    }
    
    private static void CreateQueue(OracleConnection connection, QueueInformation queue)
    {
        using var command = connection.CreateCommand();
        SetCreateQueue(command, queue);
        
        using var transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        command.ExecuteNonQuery();
        transaction.Commit();
    }
    
    private static void StartQueue(OracleConnection connection, QueueInformation queue)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "DBMS_AQADM.START_QUEUE";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new OracleParameter("queue_name", OracleDbType.Varchar2, ParameterDirection.Input) { Value = queue.Name });
        
        using var transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        command.ExecuteNonQuery();
        transaction.Commit();
    }
    
    public static void EnsureSubscriptionExists(string connectionString,
        OnMissingChannel makeChannel, 
        SubscriptionAttribute attribute)
    {
        if (makeChannel == OnMissingChannel.Assume)
        {
            return;
        }

        using var conn = new OracleConnection(connectionString);
        conn.Open();
        
        var exists = SubscriptionExists(conn, attribute);
        if (exists)
        {
            return;
        }

        if (makeChannel == OnMissingChannel.Validate)
        {
            throw new InvalidOperationException($"Subscription for '{attribute.QueueName}' queue with conumser'{attribute.ConsumerName}' doesn't exist");
        }
        
        CreateSubscription(conn, attribute);
    }

    private static bool SubscriptionExists(OracleConnection connection, SubscriptionAttribute attribute)
    {
        using var transaction = connection.BeginTransaction();
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText =
            """
            SELECT COUNT(*)
            FROM USER_QUEUE_SUBSCRIBERS
            WHERE QUEUE_NAME = @QueueName AND CONSUMER_NAME = @ConsumerName";
            """;

        cmd.Parameters.Add("@QueueName", attribute.QueueName);
        cmd.Parameters.Add("@ConsumerName", attribute.ConsumerName);
        var res = cmd.ExecuteScalar();
        return res is 1;
    }

    private static void CreateSubscription(OracleConnection connection, SubscriptionAttribute attribute)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "DBMS_AQADM.ADD_SUBSCRIBER";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new OracleParameter("queue_name", OracleDbType.Varchar2, ParameterDirection.Input) { Value = attribute.QueueName });
        command.Parameters.Add(new OracleParameter
        {
            ParameterName = "subscriber",
            Direction = ParameterDirection.Input,
            UdtTypeName = "sys.aq$_agent",
            Value = new OracleAQAgent(attribute.ConsumerName) 
        });

        if (!string.IsNullOrEmpty(attribute.Rule))
        {
            command.Parameters.Add(new OracleParameter("rule", OracleDbType.Varchar2, ParameterDirection.Input) { Value = attribute.Rule });
        }
        
        if (!string.IsNullOrEmpty(attribute.Transformation))
        {
            command.Parameters.Add(new OracleParameter("transformation", OracleDbType.Varchar2, ParameterDirection.Input) { Value = attribute.Transformation });
        }
        
        if (attribute.QueueToQueue.HasValue)
        {
            command.Parameters.Add(new OracleParameter("queue_to_queue", OracleDbType.Boolean, ParameterDirection.Input) { Value = attribute.QueueToQueue.Value });
        }
        
        if (attribute.DeliveryMode.HasValue)
        {
            command.Parameters.Add(new OracleParameter("delivery_mode", OracleDbType.Int32, ParameterDirection.Input) { Value = attribute.DeliveryMode.Value });
        }
        
        using var transaction = connection.BeginTransaction();
        command.Transaction = transaction;

        command.ExecuteNonQuery();
        transaction.Commit();
    }
    
    public static async Task EnsureExistsAsync(string connectionString, 
        OnMissingChannel makeChannel, 
        QueueAttribute attribute,
        bool startQueue = true)
    {
        if (makeChannel == OnMissingChannel.Assume)
        {
            return;
        }

#if NETFRAMEWORK
        using var conn = new OracleConnection(connectionString);
#else
        await using var conn = new OracleConnection(connectionString);
#endif
        await conn.OpenAsync();
    
        var exists = await ExistsAsync(conn, attribute.Queue.Name);
        if (exists)
        {
            return;
        }

        if (makeChannel == OnMissingChannel.Validate)
        {
            throw new InvalidOperationException($"'{attribute.Queue.Name}' doesn't exist");
        }

            
        if (attribute.Queue.ExceptionQueue != null)
        {
            await EnsureExistsAsync(connectionString, makeChannel, attribute.Queue.ExceptionQueue, false);
        }
            
        await CreateQueueTableAsync(conn, attribute.Table);
        await CreateQueueAsync(conn, attribute.Queue);
        await StartQueueAsync(conn, attribute.Queue);
    }

    private static async Task<bool> ExistsAsync(OracleConnection connection, string queue)
    {
#if NETFRAMEWORK
        using var cmd = connection.CreateCommand();
        using var transaction = connection.BeginTransaction();
#else
        await using var cmd = connection.CreateCommand();
        await using var transaction = await connection.BeginTransactionAsync();
#endif
        cmd.CommandText = "SELECT COUNT(*) FROM USER_QUEUES WHERE NAME = :p_queue_name";
        cmd.Parameters.Add(new OracleParameter("p_queue_name", queue));

        var res = await cmd.ExecuteScalarAsync();
        return res is > 0;
    }
    
    private static async Task CreateQueueTableAsync(OracleConnection connection, QueueTable table)
    {
#if NETFRAMEWORK
        using var command = connection.CreateCommand();
#else
        await using var command = connection.CreateCommand();
#endif
        SetCreateQueueTable(command, table);
        
#if NETFRAMEWORK
        using var transaction = connection.BeginTransaction();
#else
        await using var transaction = connection.BeginTransaction();
#endif
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync();
        
#if NETFRAMEWORK
        transaction.Commit();
#else
        await transaction.CommitAsync();
#endif
    }
    
    private static async Task CreateQueueAsync(OracleConnection connection, QueueInformation queue)
    {
#if NETFRAMEWORK
        using var command = connection.CreateCommand();
#else
        await using var command = connection.CreateCommand();
#endif
        SetCreateQueue(command, queue);
        
#if NETFRAMEWORK
        using var transaction = connection.BeginTransaction();
#else
        await using var transaction = connection.BeginTransaction();
#endif       
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync();
        
#if NETFRAMEWORK
        transaction.Commit();
#else
        await transaction.CommitAsync();
#endif
    }
    
    private static async Task StartQueueAsync(OracleConnection connection, QueueInformation queue)
    {
#if NETFRAMEWORK
        using var command = connection.CreateCommand();
#else
        await using var command = connection.CreateCommand();
#endif
;
        command.CommandText = "DBMS_AQADM.START_QUEUE";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new OracleParameter("queue_name", OracleDbType.Varchar2, ParameterDirection.Input) { Value = queue.Name });
        
#if NETFRAMEWORK
        using var transaction = connection.BeginTransaction();
#else
        await using var transaction = connection.BeginTransaction();
#endif

        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync();
#if NETFRAMEWORK
        transaction.Commit();
#else
        await transaction.CommitAsync();
#endif
    }
    private static async Task ExecuteAsync(OracleConnection connection, string query)
    {
#if NETFRAMEWORK
        using var command = connection.CreateCommand();
        using var transaction = connection.BeginTransaction();
#else
        await using var command = connection.CreateCommand();
        await using var transaction = await connection.BeginTransactionAsync();
#endif
        command.CommandText = query;
        await command.ExecuteNonQueryAsync();
        
#if NETFRAMEWORK
        transaction.Commit();
#else
        await transaction.CommitAsync();
#endif
    }
    
    private static void SetCreateQueueTable(OracleCommand command, QueueTable table)
    {
        command.CommandText = "DBMS_AQADM.CREATE_QUEUE_TABLE";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new OracleParameter("queue_table", OracleDbType.Varchar2, ParameterDirection.Input) { Value = table.Name });
        command.Parameters.Add(new OracleParameter("queue_payload_type", OracleDbType.Varchar2, ParameterDirection.Input) { Value = table.PayloadType });

        if (table.StorageClause != null)
        {
            command.Parameters.Add(new OracleParameter("storage_clause", OracleDbType.Varchar2, ParameterDirection.Input) { Value = table.StorageClause});
        }
        
        if (table.SortList != null)
        {
            command.Parameters.Add(new OracleParameter("sort_list", OracleDbType.Varchar2, ParameterDirection.Input) { Value = table.SortList });
        }
        
        if (table.MultipleConsumers.HasValue)
        {
            command.Parameters.Add(new OracleParameter("multiple_consumers", OracleDbType.Boolean, ParameterDirection.Input) { Value = table.MultipleConsumers.Value });
        }
        
        if (table.MessageGrouping.HasValue)
        {
            command.Parameters.Add(new OracleParameter("message_grouping", OracleDbType.Int32, ParameterDirection.Input) { Value = table.MessageGrouping.Value });
        }
        
        if (!string.IsNullOrEmpty(table.Comment))
        {
            command.Parameters.Add(new OracleParameter("comment", OracleDbType.Varchar2, ParameterDirection.Input) { Value = table.Comment });
        }
        
        if (table.AutoCommit.HasValue)
        {
            command.Parameters.Add(new OracleParameter("auto_commit", OracleDbType.Boolean, ParameterDirection.Input) { Value = table.AutoCommit.Value });
        }

        if (table.PrimaryInstance.HasValue)
        {
            command.Parameters.Add(new OracleParameter("primary_instance", OracleDbType.Int32, ParameterDirection.Input) { Value = table.PrimaryInstance.Value });
        }
        
        if (table.SecondaryInstance.HasValue)
        {
            command.Parameters.Add(new OracleParameter("secondary_instance", OracleDbType.Int32, ParameterDirection.Input) { Value = table.SecondaryInstance.Value });
        }
        
        if (!string.IsNullOrEmpty(table.Compatible))
        {
            command.Parameters.Add(new OracleParameter("compatible", OracleDbType.Varchar2, ParameterDirection.Input) { Value = table.Compatible });
        }
        
        if (table.Secure.HasValue)
        {
            command.Parameters.Add(new OracleParameter("secure", OracleDbType.Boolean, ParameterDirection.Input) { Value = table.Secure.Value });
        }
        
        table.Configuration?.Invoke(command);
    }

    private static void SetCreateQueue(OracleCommand command, QueueInformation queue)
    { 
        command.CommandText = "DBMS_AQADM.CREATE_QUEUE_TABLE";
        command.CommandType = CommandType.StoredProcedure;
        
        command.Parameters.Add(new OracleParameter("queue_name", OracleDbType.Varchar2, ParameterDirection.Input) { Value = queue.Name });
        command.Parameters.Add(new OracleParameter("queue_table", OracleDbType.Varchar2, ParameterDirection.Input) { Value = queue.Table });
        
        if (queue.QueueType.HasValue)
        {
            command.Parameters.Add(new OracleParameter("queue_type", OracleDbType.Int32, ParameterDirection.Input) { Value = queue.QueueType.Value });
        }
        
        if (queue.MaxRetries.HasValue)
        {
            command.Parameters.Add(new OracleParameter("max_retries", OracleDbType.Int32, ParameterDirection.Input) { Value = queue.MaxRetries.Value });
        }
        
        if (queue.RetryDelay.HasValue)
        {
            command.Parameters.Add(new OracleParameter("retry_delay", OracleDbType.Int32, ParameterDirection.Input) { Value = queue.RetryDelay.Value });
        }
        
        if (queue.RetentionTime.HasValue)
        {
            command.Parameters.Add(new OracleParameter("retention_time", OracleDbType.Int32, ParameterDirection.Input) { Value = queue.RetentionTime.Value });
        }

        if (queue.DependencyTracking.HasValue)
        {
            command.Parameters.Add(new OracleParameter("dependency_tracking", OracleDbType.Boolean, ParameterDirection.Input) { Value = queue.DependencyTracking.Value });
        }
        
        if (!string.IsNullOrEmpty(queue.Comment))
        {
            command.Parameters.Add(new OracleParameter("comment", OracleDbType.Varchar2, ParameterDirection.Input) { Value = queue.Comment });
        }
        
        if (queue.AutoCommit.HasValue)
        {
            command.Parameters.Add(new OracleParameter("auto_commit", OracleDbType.Boolean, ParameterDirection.Input) { Value = queue.AutoCommit.Value });
        }
        
        queue.Configuration?.Invoke(command);
    }
}
