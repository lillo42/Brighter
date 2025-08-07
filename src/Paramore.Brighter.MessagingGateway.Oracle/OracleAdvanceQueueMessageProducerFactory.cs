using System.Collections.Generic;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

public class OracleAdvanceQueueMessageProducerFactory(OraclesMessagingGatewayConnection connection, IEnumerable<OracleAdvanceQueuePublication> publications) : IAmAMessageProducerFactory
{
    /// <inheritdoc />
    public Dictionary<ProducerKey, IAmAMessageProducer> Create()
    {
        var producers = new Dictionary<ProducerKey, IAmAMessageProducer>();
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

            publication.Sender ??= connection.DefaultSender;
            QueueFactory.EnsureExists(connection.Configuration.ConnectionString, 
                publication.MakeChannels, 
                GetOrCreateQueueAttribute(publication));
            
            producers[new ProducerKey(publication.Topic, publication.Type)] = new OracleAdvanceQueueMessageProducer(
                connection.Configuration.ConnectionString, publication);
        }
        
        return producers;
    }
    
    /// <inheritdoc />
    public async Task<Dictionary<ProducerKey, IAmAMessageProducer>> CreateAsync()
    {
        var producers = new Dictionary<ProducerKey, IAmAMessageProducer>();
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

            publication.Sender ??= connection.DefaultSender;
            await QueueFactory.EnsureExistsAsync(connection.Configuration.ConnectionString, 
                publication.MakeChannels, 
                GetOrCreateQueueAttribute(publication));
            
            producers[new ProducerKey(publication.Topic, publication.Type)] = new OracleAdvanceQueueMessageProducer(
                connection.Configuration.ConnectionString,
                publication);
        }
        
        return producers;
    }

    private static QueueAttribute GetOrCreateQueueAttribute(OracleAdvanceQueuePublication publication)
    {
        var attribute = publication.Attribute ?? new QueueAttribute();
        if (string.IsNullOrEmpty(attribute.Queue.Name))
        {
            attribute.Queue.Name = publication.Queue;
        }

        if (string.IsNullOrEmpty(attribute.Table.Name))
        {
            attribute.Table.Name = attribute.Queue.Name + "_TABLE";
        }
        
        if (string.IsNullOrEmpty(attribute.Queue.Table))
        {
            attribute.Queue.Table = attribute.Table.Name;
        }

        if (string.IsNullOrEmpty(attribute.Table.PayloadType))
        {
            attribute.Table.PayloadType = publication.MessageType switch
            {
                OracleAQMessageType.Udt when !string.IsNullOrEmpty(publication.UdtTypeName) => publication.UdtTypeName!,
                OracleAQMessageType.Udt => throw new ConfigurationException("A Oracle AQ publication must have a UdtTypeName"),
                _ => publication.MessageType.ToString().ToUpper()
            };
        }

        if (attribute.Queue.ExceptionQueue is { Queue.QueueType: null })
        {
            attribute.Queue.ExceptionQueue.Queue.QueueType = "DBMS_AQADM.EXCEPTION_QUEUE";
        }

        return attribute;
    }
}
