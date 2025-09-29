namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// A factory responsible for creating synchronous and asynchronous ActiveMQ message consumers
/// that implement Brighter's <see cref="IAmAMessageConsumerSync"/> and <see cref="IAmAMessageConsumerAsync"/> interfaces.
/// </summary>
/// <remarks>
/// This factory centralizes the logic for establishing the necessary ActiveMQ connection and
/// creating the underlying consumer based on the provided subscription details.
/// </remarks>
public class ActiveMqMessageConsumerFactory(ActiveMqMessagingGatewayConnection gatewayConnection) : IAmAMessageConsumerFactory
{
    /// <summary>
    /// Creates a synchronous ActiveMQ message consumer.
    /// </summary>
    /// <param name="subscription">The subscription details, which must be an <see cref="ActiveMqTopicSubscription"/> or <see cref="ActiveMqQueueSubscription"/>.</param>
    /// <returns>
    /// A synchronous message consumer (<see cref="IAmAMessageConsumerSync"/>) configured for ActiveMQ.
    /// </returns>
    public IAmAMessageConsumerSync Create(Subscription subscription)
    {
        return CreateActiveMqMessageConsumer(subscription);
    }

    /// <summary>
    /// Creates an asynchronous ActiveMQ message consumer.
    /// </summary>
    /// <param name="subscription">The subscription details, which must be an <see cref="ActiveMqTopicSubscription"/> or <see cref="ActiveMqQueueSubscription"/>.</param>
    /// <returns>
    /// An asynchronous message consumer (<see cref="IAmAMessageConsumerAsync"/>) configured for ActiveMQ.
    /// </returns>
    public IAmAMessageConsumerAsync CreateAsync(Subscription subscription)
    {
        return CreateActiveMqMessageConsumer(subscription);
    }

    /// <summary>
    /// Creates the concrete ActiveMQ message consumer implementation.
    /// </summary>
    /// <param name="subscription">The subscription used to configure the consumer.</param>
    /// <returns>
    /// A new instance of <see cref="ActiveMqMessageConsumer"/>.
    /// </returns>
    /// <exception cref="ConfigurationException">
    /// Thrown if the <paramref name="subscription"/> parameter is not an <see cref="ActiveMqTopicSubscription"/> or <see cref="ActiveMqQueueSubscription"/>.
    /// </exception>
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
