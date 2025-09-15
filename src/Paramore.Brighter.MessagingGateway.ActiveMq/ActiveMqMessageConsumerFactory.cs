namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqMessageConsumerFactory(ActiveMqMessagingGatewayConnection gatewayConnection) : IAmAMessageConsumerFactory
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

        var connection = gatewayConnection.GetConnection();
        return new ActiveMqMessageConsumer(connection, (ActiveMqSubscription)subscription);
    }
}
