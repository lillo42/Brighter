using System;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

internal static class QueueFactory
{
    public static void EnsureExists(string connectionString, 
        OnMissingChannel makeChannel, 
        QueueAttribute attribute,
        bool startQueue = true)
    {
        if (makeChannel == OnMissingChannel.Assume)
        {
            return;
        }

        using var conn = new OracleConnection(connectionString);
        conn.Open();
        
        var exists = Exist(conn, attribute.Queue.Name);
        if (makeChannel == OnMissingChannel.Validate && exists)
        {
            return;
        }

        if (makeChannel == OnMissingChannel.Validate && !exists)
        {
            throw new InvalidOperationException($"'{attribute.Queue.Name}' doesn't exist");
        }

        if (exists && !attribute.OverrideIfNotExists)
        {
            return;
        }

        if (exists)
        {
            if (attribute.Queue.ExceptionQueue != null)
            {
                Execute(conn, $"DBMS_AQADM.STOP_QUEUE(queue_name => '{attribute.Queue.ExceptionQueue.Queue.Name}')");
            }
            
            Execute(conn, $"DBMS_AQADM.STOP_QUEUE(queue_name => '{attribute.Queue.Name}')");
            
            if (attribute.Queue.ExceptionQueue != null)
            {
                EnsureExists(connectionString, makeChannel, attribute.Queue.ExceptionQueue, false);
            }
            
            Execute(conn, ChangeQueueTableQuery(attribute.Table));
            Execute(conn, ChangeQueueQuery(attribute.Queue));
            
            if (!string.IsNullOrEmpty(attribute.Table.PayloadTypeCreateOrUpdateQuery))
            {
                Execute(conn, attribute.Table.PayloadTypeCreateOrUpdateQuery!);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(attribute.Table.PayloadTypeCreateOrUpdateQuery))
            {
                Execute(conn, attribute.Table.PayloadTypeCreateOrUpdateQuery!);
            }
            
            if (attribute.Queue.ExceptionQueue != null)
            {
                EnsureExists(connectionString, makeChannel, attribute.Queue.ExceptionQueue, false);
            }
            
            Execute(conn, CreateQueueTableQuery(attribute.Table));
            Execute(conn, CreateQueueQuery(attribute.Queue));
        }

        if (!startQueue)
        {
            return;
        }
        
        if (attribute.Queue.ExceptionQueue != null)
        {
            Execute(conn, $"DBMS_AQADM.START_QUEUE(queue_name => '{attribute.Queue.ExceptionQueue.Queue.Name}')");
        }
            
        Execute(conn, $"DBMS_AQADM.START_QUEUE(queue_name => '{attribute.Queue.Name}')");
    }

    private static bool Exist(OracleConnection connection, string queue)
    {
        using var cmd = connection.CreateCommand();
        using var transaction = connection.BeginTransaction();
        cmd.CommandText = "SELECT COUNT(*) FROM USER_QUEUES WHERE NAME = :p_queue_name";
        cmd.Parameters.Add(new OracleParameter("p_queue_name", queue));

        var res = cmd.ExecuteScalar();
        return res is > 0;
    }

    private static void Execute(OracleConnection connection, string query)
    {
        using var command = connection.CreateCommand();
        using var transaction = connection.BeginTransaction();
        command.CommandText = query;
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
    
        var exists = Exist(conn, attribute.Queue.Name);
        if (makeChannel == OnMissingChannel.Validate && exists)
        {
            return;
        }

        if (makeChannel == OnMissingChannel.Validate && !exists)
        {
            throw new InvalidOperationException($"'{attribute.Queue.Name}' doesn't exist");
        }

        if (exists && !attribute.OverrideIfNotExists)
        {
            return;
        }

        if (exists)
        {
            if (attribute.Queue.ExceptionQueue != null)
            {
                await EnsureExistsAsync(connectionString, makeChannel, attribute.Queue.ExceptionQueue, false);
            }
            
            if (!string.IsNullOrEmpty(attribute.Table.PayloadTypeCreateOrUpdateQuery))
            {
                await ExecuteAsync(conn, attribute.Table.PayloadTypeCreateOrUpdateQuery!);
            }
            
            await ExecuteAsync(conn, $"DBMS_AQADM.STOP_QUEUE(queue_name => '{attribute.Queue.Name}')");
            await ExecuteAsync(conn, ChangeQueueTableQuery(attribute.Table));
            await ExecuteAsync(conn, ChangeQueueQuery(attribute.Queue));
        }
        else
        {
            if (!string.IsNullOrEmpty(attribute.Table.PayloadTypeCreateOrUpdateQuery))
            {
                await ExecuteAsync(conn, attribute.Table.PayloadTypeCreateOrUpdateQuery!);
            }
            
            if (attribute.Queue.ExceptionQueue != null)
            {
                await EnsureExistsAsync(connectionString, makeChannel, attribute.Queue.ExceptionQueue, false);
            }
            
            await ExecuteAsync(conn, CreateQueueTableQuery(attribute.Table));
            await ExecuteAsync(conn, CreateQueueQuery(attribute.Queue));
        }

        if (!startQueue)
        {
            return;
        }
        
        if (attribute.Queue.ExceptionQueue != null)
        {
            await ExecuteAsync(conn, $"DBMS_AQADM.START_QUEUE(queue_name => '{attribute.Queue.ExceptionQueue.Queue.Name}')");
        }
            
        await ExecuteAsync(conn, $"DBMS_AQADM.START_QUEUE(queue_name => '{attribute.Queue.Name}')");
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
    
    private static string CreateQueueTableQuery(QueueTable table)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DBMS_AQADM.CREATE_QUEUE_TABLE(");
        sb.Append(' ', 2).AppendLine($"queue_table         => '{table.Name}',");
        sb.Append(' ', 2).Append($"queue_payload_type  => '{table.PayloadType}'");
        
        if (!string.IsNullOrEmpty(table.StorageClause))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"storage_clause      => '{table.StorageClause}'");
        }

        if (!string.IsNullOrEmpty(table.SortList))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"sort_list           => '{table.SortList}'");
        }
        
        if (table.MultipleConsumers.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"multiple_consumers  => {table.MultipleConsumers.Value.ToString().ToUpper()}");
        }
        
        if (table.MessageGrouping.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"message_grouping    => {table.MessageGrouping}");
        }
        
        if (!string.IsNullOrEmpty(table.Comment))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"comment             => '{table.Comment}'");
        }
        
        if (table.AutoCommit.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"auto_commit         => {table.AutoCommit.Value.ToString().ToUpper()}");
        }    
        
        if (table.PrimaryInstance.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"primary_instance    => {table.PrimaryInstance.Value}");
        }
        
        if (table.SecondaryInstance.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"secondary_instance  => {table.SecondaryInstance.Value}");
        }
        
        if (!string.IsNullOrEmpty(table.Compatible))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"compatible          => '{table.Compatible}'");
        }
        
        if (table.Secure.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"secure              => {table.Secure.Value.ToString().ToUpper()}");
        }
        
        if (table.RetentionTime.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"retention_time      => {table.RetentionTime.Value.ToString()}");
        }
        
        if (table.DependencyTracking.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"dependency_tracking => {table.DependencyTracking.Value.ToString().ToUpper()}");
        }
        
        if (table.MaxRetries.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"max_retries         => {table.MaxRetries.Value.ToString()}");
        }
        
        if (table.RetryDelay.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"retry_delay         => {table.RetryDelay.Value.ToString()}");
        }
        
        if (!string.IsNullOrEmpty(table.Users))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"users               => '{table.Users}'");
        }
        
        if (!string.IsNullOrEmpty(table.UserComment))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"user_comment        => '{table.UserComment}'");
        }
        
        table.Configuration?.Invoke(sb);
        
        sb.AppendLine().Append(");");
        return sb.ToString();
    }

    private static string ChangeQueueTableQuery(QueueTable table)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DBMS_AQADM.ALTER_QUEUE_TABLE(");
        sb.Append(' ', 2).AppendLine($"queue_table         => '{table.Name}',");
        sb.Append(' ', 2).Append($"queue_payload_type  => '{table.PayloadType.ToString().ToUpper()}'");
        
        if (!string.IsNullOrEmpty(table.StorageClause))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"storage_clause      => '{table.StorageClause}'");
        }

        if (!string.IsNullOrEmpty(table.SortList))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"sort_list           => '{table.SortList}'");
        }
        
        if (table.MultipleConsumers.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"multiple_consumers  => {table.MultipleConsumers.Value.ToString().ToUpper()}");
        }
        
        if (table.MessageGrouping.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"message_grouping    => {table.MessageGrouping}");
        }
        
        if (!string.IsNullOrEmpty(table.Comment))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"comment             => '{table.Comment}'");
        }
        
        if (table.AutoCommit.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"auto_commit         => {table.AutoCommit.Value.ToString().ToUpper()}");
        }    
        
        if (table.PrimaryInstance.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"primary_instance    => {table.PrimaryInstance.Value}");
        }
        
        if (table.SecondaryInstance.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"secondary_instance  => {table.SecondaryInstance.Value}");
        }
        
        if (!string.IsNullOrEmpty(table.Compatible))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"compatible          => '{table.Compatible}'");
        }
        
        if (table.Secure.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"secure              => {table.Secure.Value.ToString().ToUpper()}");
        }
        
        if (table.RetentionTime.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"retention_time      => {table.RetentionTime.Value.ToString()}");
        }
        
        if (table.DependencyTracking.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"dependency_tracking => {table.DependencyTracking.Value.ToString().ToUpper()}");
        }
        
        if (table.MaxRetries.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"max_retries         => {table.MaxRetries.Value.ToString()}");
        }
        
        if (table.RetryDelay.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"retry_delay         => {table.RetryDelay.Value.ToString()}");
        }
        
        if (!string.IsNullOrEmpty(table.Users))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"users               => '{table.Users}'");
        }
        
        if (!string.IsNullOrEmpty(table.UserComment))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"user_comment        => '{table.UserComment}'");
        }
        
        table.Configuration?.Invoke(sb);
        
        sb.AppendLine().Append(");");
        return sb.ToString();
    }
    
     private static string CreateQueueQuery(QueueInformation queue)
     {
        var sb = new StringBuilder();
        sb.AppendLine("DBMS_AQADM.ALTER_QUEUE(");
        sb.Append(' ', 2).AppendLine($"queue_name         => '{queue.Name}',");
        sb.Append(' ', 2).Append($"queue_table        => '{queue.Table}'");
        
        if (queue.QueueType != null)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"queue_type         => '{queue.QueueType}'");
        }

        if (!string.IsNullOrEmpty(queue.Comment))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"comment             => '{queue.Comment}'");
        }
        
        if (queue.AutoCommit.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"auto_commit         => {queue.AutoCommit.Value.ToString().ToUpper()}");
        }    
        
        if (queue.PrimaryInstance.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"primary_instance    => {queue.PrimaryInstance.Value}");
        }
        
        if (queue.SecondaryInstance.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"secondary_instance  => {queue.SecondaryInstance.Value}");
        }
        
        if (!string.IsNullOrEmpty(queue.Compatible))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"compatible          => '{queue.Compatible}'");
        }
        
        if (queue.Secure.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"secure              => {queue.Secure.Value.ToString().ToUpper()}");
        }
        
        if (queue.RetentionTime.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"retention_time      => {queue.RetentionTime.Value.ToString()}");
        }
        
        if (queue.DependencyTracking.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"dependency_tracking => {queue.DependencyTracking.Value.ToString().ToUpper()}");
        }
        
        if (queue.MaxRetries.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"max_retries         => {queue.MaxRetries.Value.ToString()}");
        }
        
        if (queue.RetryDelay.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"retry_delay         => {queue.RetryDelay.Value.ToString()}");
        }
        
        if (queue.ExceptionQueue != null)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"exception_queue     => '{queue.ExceptionQueue.Queue.Name}'");
        }
        
        
        queue.Configuration?.Invoke(sb);
        
        sb.AppendLine().Append(");");
        return sb.ToString();
    }
     
    private static string ChangeQueueQuery(QueueInformation queue)
    { 
        var sb = new StringBuilder();
        sb.AppendLine("DBMS_AQADM.CREATE_QUEUE(");
        sb.Append(' ', 2).AppendLine($"queue_name         => '{queue.Name}',");
        sb.Append(' ', 2).Append($"queue_table        => '{queue.Table}'");
        
        if (queue.QueueType != null)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"queue_type         => '{queue.QueueType}'");
        }

        if (!string.IsNullOrEmpty(queue.Comment))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"comment             => '{queue.Comment}'");
        }
        
        if (queue.AutoCommit.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"auto_commit         => {queue.AutoCommit.Value.ToString().ToUpper()}");
        }    
        
        if (queue.PrimaryInstance.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"primary_instance    => {queue.PrimaryInstance.Value}");
        }
        
        if (queue.SecondaryInstance.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"secondary_instance  => {queue.SecondaryInstance.Value}");
        }
        
        if (!string.IsNullOrEmpty(queue.Compatible))
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"compatible          => '{queue.Compatible}'");
        }
        
        if (queue.Secure.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"secure              => {queue.Secure.Value.ToString().ToUpper()}");
        }
        
        if (queue.RetentionTime.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"retention_time      => {queue.RetentionTime.Value.ToString()}");
        }
        
        if (queue.DependencyTracking.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"dependency_tracking => {queue.DependencyTracking.Value.ToString().ToUpper()}");
        }
        
        if (queue.MaxRetries.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"max_retries         => {queue.MaxRetries.Value.ToString()}");
        }
        
        if (queue.RetryDelay.HasValue)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"retry_delay         => {queue.RetryDelay.Value.ToString()}");
        }
        
        if (queue.ExceptionQueue != null)
        {
            sb.AppendLine(",");
            sb.Append(' ', 2).Append($"exception_queue     => '{queue.ExceptionQueue.Queue.Name}'");
        }
        
        queue.Configuration?.Invoke(sb);
        
        sb.AppendLine().Append(");");
        return sb.ToString();
    }
}
