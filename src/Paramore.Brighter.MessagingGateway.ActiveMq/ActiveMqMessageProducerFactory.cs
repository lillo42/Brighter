using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqMessageProducerFactory(ActiveMqMessagingGatewayConnection connection, IEnumerable<ActiveMqPublication> publications) : IAmAMessageProducerFactory
{
    /// <inheritdoc />
    public Dictionary<ProducerKey, IAmAMessageProducer> Create()
    {
        var conn = connection.GetConnection();

        var producers = new Dictionary<ProducerKey, IAmAMessageProducer>();
        foreach (var publication in publications)
        {
            if (RoutingKey.IsNullOrEmpty(publication.Topic))
            {
                throw new ConfigurationException("Topic can't be empty");
            }
            

            producers[new ProducerKey(publication.Topic, publication.Type)] = new ActiveMqMessageProducer(conn,
                publication, 
                publication.TimeProvider ?? connection.TimeProvider);
        }

        return producers;
    }

    /// <inheritdoc />
    public async Task<Dictionary<ProducerKey, IAmAMessageProducer>> CreateAsync()
    {
        var conn = await connection.GetConnectionAsync();
        var producers = new Dictionary<ProducerKey, IAmAMessageProducer>();
        foreach (var publication in publications)
        {
            if (RoutingKey.IsNullOrEmpty(publication.Topic))
            {
                throw new ConfigurationException("Topic can't be empty");
            }

            producers[new ProducerKey(publication.Topic, publication.Type)] = new ActiveMqMessageProducer(conn,
                publication, 
                publication.TimeProvider ?? connection.TimeProvider);
        }

        return producers;
    }
}
