namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// Specifies the type of consumer to be created for a Topic subscription in ActiveMQ (via the NMS API).
/// </summary>
public enum ConsumerType
{
    /// <summary>
    /// The default consumer type. This is a **non-durable** consumer, meaning the broker
    /// will not retain messages for it while it is disconnected.
    /// </summary>
    Default,

    /// <summary>
    /// A **durable** consumer. The broker will retain and deliver messages for this consumer
    /// even if the consumer is temporarily disconnected. Requires a unique <see cref="P:Subscription.Name"/>.
    /// </summary>
    Durable,

    /// <summary>
    /// A **shared non-durable** consumer. Multiple consumers can subscribe to the same topic
    /// with the same consumer name, and the messages will be load-balanced among them.
    /// Messages are not retained upon disconnection.
    /// </summary>
    Share,

    /// <summary>
    /// A **shared durable** consumer. Multiple consumers can subscribe to the same topic
    /// with the same consumer name, and the messages will be load-balanced among them.
    /// Messages are retained upon disconnection. Requires a unique <see cref="P:Paramore.Brighter.Subscription.Name"/>.
    /// </summary>
    ShareDurable
}
