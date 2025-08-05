using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Paramore.Brighter.Oracle;

namespace Paramore.Brighter.MessagingGateway.Oracle;

public class OracleAdvanceQueueMessageProducerFactory(OraclesMessagingGatewayConnection connection, IEnumerable<OracleAdvanceQueuePublication> publications) : IAmAMessageProducerFactory
{
    /// <inheritdoc />
    public Dictionary<RoutingKey, IAmAMessageProducer> Create()
    {
        var producers = new Dictionary<RoutingKey, IAmAMessageProducer>();
        foreach (var publication in publications)
        {
            if (RoutingKey.IsNullOrEmpty(publication.Topic))
            {
                throw new ConfigurationException("A Oracle AQ publication must have a topic");
            }

            if (string.IsNullOrEmpty(publication.Queue))
            {
                throw new ConfigurationException("A Oracle AQ publication must have a queue");
            }

            publication.Sender ??= connection.Sender;
            EnsureQueueExists(publication);
            
            producers[publication.Topic] = new OracleAdvanceQueueMessageProducer(
                new OracleConnectionProvider(connection.Configuration),
                publication);
        }
        
        return producers;
    }
    
    private void EnsureQueueExists(OracleAdvanceQueuePublication publication)
    {
        if (publication.MakeChannels == OnMissingChannel.Assume)
        {
            return;
        }

        bool exists = false;
        using var conn = new OracleConnection(connection.Configuration.ConnectionString);
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM USER_QUEUES WHERE NAME = :p_queue_name";
            cmd.Parameters.Add(new OracleParameter("p_queue_name", publication.Queue));
        
            var res = cmd.ExecuteScalar();
            exists = res is > 0;
        }
        
        if (publication.MakeChannels == OnMissingChannel.Validate)
        {
            if (exists)
            {
                return;
            }

            throw new InvalidOperationException($"'{publication.Queue}' doesn't exist");
        }
        
    }
    
    /// <inheritdoc />
    public async Task<Dictionary<RoutingKey, IAmAMessageProducer>> CreateAsync()
    {
        var producers = new Dictionary<RoutingKey, IAmAMessageProducer>();
        foreach (var publication in publications)
        {
            if (RoutingKey.IsNullOrEmpty(publication.Topic))
            {
                throw new ConfigurationException("A Oracle AQ publication must have a topic");
            }

            if (string.IsNullOrEmpty(publication.Queue))
            {
                throw new ConfigurationException("A Oracle AQ publication must have a queue");
            }

            publication.Sender ??= connection.Sender;
            await EnsureQueueExistsAsync(publication);
            
            producers[publication.Topic] = new OracleAdvanceQueueMessageProducer(
                new OracleConnectionProvider(connection.Configuration),
                publication);
        }
        
        return producers;
    }

    private async Task EnsureQueueExistsAsync(OracleAdvanceQueuePublication publication)
    {
        if (publication.MakeChannels == OnMissingChannel.Assume)
        {
            return;
        }

#if NETFRAMEWORK
        using var conn = new OracleConnection(connection.Configuration.ConnectionString);
#else
        await using var conn = new OracleConnection(connection.Configuration.ConnectionString);
#endif
        await conn.OpenAsync();
        
#if NETFRAMEWORK
        using var cmd = conn.CreateCommand();
#else
        await using var cmd = conn.CreateCommand();
#endif
        cmd.CommandText = "SELECT COUNT(*) FROM USER_QUEUES WHERE NAME = :p_queue_name";
        cmd.Parameters.Add(new OracleParameter("p_queue_name", publication.Queue));
        
        var res = await cmd.ExecuteScalarAsync();
        var exists = res is > 0;
        if (publication.MakeChannels == OnMissingChannel.Validate)
        {
            if (exists)
            {
                return;
            }

            throw new InvalidOperationException($"'{publication.Queue}' doesn't exist");
        }
        
        
    }   
}
