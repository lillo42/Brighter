using System.Collections.Generic;
using System.Threading.Tasks;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// A factory responsible for creating a set of ActiveMQ message producers, 
/// indexed by their <see cref="ProducerKey"/>, based on a provided collection of 
/// <see cref="ActiveMqPublication"/> configurations.
/// </summary>
public class ActiveMqMessageProducerFactory(ActiveMqMessagingGatewayConnection connection, IEnumerable<ActiveMqPublication> publications) : IAmAMessageProducerFactory
{
    /// <summary>
    /// Creates a synchronous collection of ActiveMQ message producers.
    /// </summary>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> where the key is a <see cref="ProducerKey"/> 
    /// (combining the topic and request type) and the value is the configured <see cref="IAmAMessageProducer"/>.
    /// </returns>
    /// <exception cref="ConfigurationException">
    /// Thrown if any publication in the collection has a <see cref="P:Paramore.Brighter.Publication.Topic"/> 
    /// that is null or empty.
    /// </exception>
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

    /// <summary>
    /// Asynchronously creates a collection of ActiveMQ message producers.
    /// </summary>
    /// <returns>
    /// A task that returns a <see cref="Dictionary{TKey, TValue}"/> where the key is a <see cref="ProducerKey"/> 
    /// and the value is the configured <see cref="IAmAMessageProducer"/>.
    /// </returns>
    /// <exception cref="ConfigurationException">
    /// Thrown if any publication in the collection has a <see cref="P:Paramore.Brighter.Publication.Topic"/> 
    /// that is null or empty.
    /// </exception>
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
