using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// A factory for creating Brighter <see cref="IAmAChannelSync"/> and <see cref="IAmAChannelAsync"/>
/// instances that are configured to communicate with an ActiveMQ message broker.
/// </summary>
/// <remarks>
/// This implementation relies on an internal <see cref="ActiveMqMessageConsumerFactory"/>
/// to handle the complexities of creating the underlying ActiveMQ message consumers (e.g., NMS consumers).
/// </remarks>
public class ActiveMqChannelFactory(ActiveMqMessagingGatewayConnection gatewayConnection) : IAmAChannelFactory
{
    private readonly ActiveMqMessageConsumerFactory _factory = new(gatewayConnection);
    
    /// <summary>
    /// Creates a synchronous channel configured for a specific subscription.
    /// </summary>
    /// <param name="subscription">The subscription details required for the channel (e.g., queue name, buffer size).</param>
    /// <returns>
    /// A synchronous Brighter channel implementation (<see cref="IAmAChannelSync"/>) backed by an ActiveMQ consumer.
    /// </returns>
    public IAmAChannelSync CreateSyncChannel(Subscription subscription)
    {
        return new Channel(
            subscription.ChannelName,
            subscription.RoutingKey,
            _factory.Create(subscription),
            subscription.BufferSize);
    }

    /// <summary>
    /// Creates an asynchronous channel configured for a specific subscription.
    /// </summary>
    /// <param name="subscription">The subscription details required for the channel.</param>
    /// <returns>
    /// An asynchronous Brighter channel implementation (<see cref="IAmAChannelAsync"/>) backed by an ActiveMQ consumer.
    /// </returns>
    public IAmAChannelAsync CreateAsyncChannel(Subscription subscription)
    {
        return new ChannelAsync(
            subscription.ChannelName,
            subscription.RoutingKey,
            _factory.CreateAsync(subscription),
            subscription.BufferSize);
    }

    /// <summary>
    /// Creates an asynchronous channel configured for a specific subscription.
    /// </summary>
    /// <param name="subscription">The subscription details required for the channel.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// A task that returns an asynchronous Brighter channel implementation (<see cref="IAmAChannelAsync"/>) 
    /// backed by an ActiveMQ consumer.
    /// </returns>
    public Task<IAmAChannelAsync> CreateAsyncChannelAsync(Subscription subscription, CancellationToken ct = default)
    {
        return Task.FromResult<IAmAChannelAsync>(new ChannelAsync(
            subscription.ChannelName,
            subscription.RoutingKey,
            _factory.CreateAsync(subscription),
            subscription.BufferSize));
    }
}
