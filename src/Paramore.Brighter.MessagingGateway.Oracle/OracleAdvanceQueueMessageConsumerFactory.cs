using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

public class OracleAdvanceQueueMessageConsumerFactory(OraclesMessagingGatewayConnection connection) : IAmAMessageConsumerFactory
{
    /// <inheritdoc />
    public IAmAMessageConsumerSync Create(Subscription subscription) 
        => CreatePullMessageConsumer(subscription);

    /// <inheritdoc />
    public IAmAMessageConsumerAsync CreateAsync(Subscription subscription) 
        => CreatePullMessageConsumer(subscription);

    private OracleAdvanceQueuePullMessageConsumer CreatePullMessageConsumer(Subscription subscription)
    {
        if (subscription is not OracleAdvanceQueueSubscription oracleSubscription)
        {
            throw new ConfigurationException("Expecting OracleAdvanceQueueSubscription or OracleAdvanceQueueSubscription<T>");
        }

        QueueFactory.EnsureExists(connection.Configuration.ConnectionString,
            subscription.MakeChannels,
            GetOrCreateQueueAttribute(oracleSubscription));

        return new OracleAdvanceQueuePullMessageConsumer(connection.Configuration.ConnectionString,
            oracleSubscription,
            oracleSubscription.Encoding ?? connection.DefaultEncoding);
    }

    private QueueAttribute GetOrCreateQueueAttribute(OracleAdvanceQueueSubscription subscription)
    {
        var attribute = subscription.Attribute ?? new QueueAttribute();
        if (string.IsNullOrEmpty(attribute.Queue.Name))
        {
            attribute.Queue.Name = subscription.ChannelName.Value;
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
            attribute.Table.PayloadType = subscription.MessageType switch
            {
                OracleAQMessageType.Udt when !string.IsNullOrEmpty(subscription.UdtTypeName) => subscription.UdtTypeName!,
                OracleAQMessageType.Udt => throw new ConfigurationException("A Oracle AQ Subscription must have a UdtTypeName"),
                _ => subscription.MessageType.ToString().ToUpper()
            };
        }

        if (attribute.Queue.ExceptionQueue is { Queue.QueueType: null })
        {
            attribute.Queue.ExceptionQueue.Queue.QueueType = "DBMS_AQADM.EXCEPTION_QUEUE";
        }

        return attribute;
    }
}
