namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqMessageConsumerFactory(ActiveMqMessagingGateway gateway) : IAmAMessageConsumerFactory
{
    /// <inheritdoc />
    public IAmAMessageConsumerSync Create(Subscription subscription)
        => CreateActiveMqMessageConsumer(subscription);

    /// <inheritdoc />
    public IAmAMessageConsumerAsync CreateAsync(Subscription subscription) 
        => CreateActiveMqMessageConsumer(subscription);

    private ActiveMqMessageConsumer CreateActiveMqMessageConsumer(Subscription subscription)
    {
        if (subscription is not (ActiveMqTopicSubscription or ActiveMqQueueSubscription))
        {
            throw new ConfigurationException("Excepting ActiveMqTopicSubscription or ActiveMqQueueSubscription");
        }

        var connection = gateway.GetConnection();
        return new ActiveMqMessageConsumer(connection, (ActiveMqSubscription)subscription);
    }
}
