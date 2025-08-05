using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqMessageProducerFactory(ActiveMqMessagingGateway connection, IEnumerable<ActiveMqPublication> publications) : IAmAMessageProducerFactory
{
    /// <inheritdoc />
    public Dictionary<RoutingKey, IAmAMessageProducer> Create()
    {
        var conn = connection.ConnectionFactory.CreateConnection();
        
        var producers = new Dictionary<RoutingKey, IAmAMessageProducer>();
        foreach (var publication in publications)
        {
            if (RoutingKey.IsNullOrEmpty(publication.Topic))
            {
                throw new ConfigurationException("Topic can't be empty");
            }

            using var session = conn.CreateSession();
            var destination = GetDestination(session, publication);
            producers[publication.Topic] = new ActiveMqMessageProducer(conn,
                publication, 
                destination!,
                publication.TimeProvider ?? connection.TimeProvider);
        }

        return producers;

        static IDestination? GetDestination(ISession session, ActiveMqPublication publication)
        {
            return publication switch
            {
                ActiveMqTopicPublication => session.GetTopic(publication.Topic!.Value),
                ActiveMqQueuePublication => session.GetQueue(publication.Topic!.Value),
                _ => null
            };
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<RoutingKey, IAmAMessageProducer>> CreateAsync()
    {
        var conn = await connection.ConnectionFactory.CreateConnectionAsync();
        var producers = new Dictionary<RoutingKey, IAmAMessageProducer>();
        foreach (var publication in publications)
        {
            if (RoutingKey.IsNullOrEmpty(publication.Topic))
            {
                throw new ConfigurationException("Topic can't be empty");
            }

            using var session = await conn.CreateSessionAsync();
            var destination = await GetDestinationAsync(session, publication);
            producers[publication.Topic] = new ActiveMqMessageProducer(conn,
                publication, 
                destination!,
                publication.TimeProvider ?? connection.TimeProvider);
        }

        return producers;

        static async Task<IDestination?> GetDestinationAsync(ISession session, ActiveMqPublication publication)
        {
            return publication switch
            {
                ActiveMqTopicPublication => await session.GetTopicAsync(publication.Topic!.Value),
                ActiveMqQueuePublication => await session.GetQueueAsync(publication.Topic!.Value),
                _ => null
            };
        }
    }
}
