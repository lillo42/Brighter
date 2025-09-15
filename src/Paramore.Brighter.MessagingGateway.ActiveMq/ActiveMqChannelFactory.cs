using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqChannelFactory(ActiveMqMessagingGatewayConnection gatewayConnection) : IAmAChannelFactory
{
    private readonly ActiveMqMessageConsumerFactory _factory = new(gatewayConnection);
    
    /// <inheritdoc />
    public IAmAChannelSync CreateSyncChannel(Subscription subscription)
        => new Channel(
            subscription.ChannelName,
            subscription.RoutingKey,
            _factory.Create(subscription),
            subscription.BufferSize);

    /// <inheritdoc />
    public IAmAChannelAsync CreateAsyncChannel(Subscription subscription)
        => new ChannelAsync(
                subscription.ChannelName,
                subscription.RoutingKey,
                _factory.CreateAsync(subscription),
                subscription.BufferSize);

    /// <inheritdoc />
    public Task<IAmAChannelAsync> CreateAsyncChannelAsync(Subscription subscription, CancellationToken ct = default)
        => Task.FromResult<IAmAChannelAsync>(new ChannelAsync(
            subscription.ChannelName,
            subscription.RoutingKey,
            _factory.CreateAsync(subscription),
            subscription.BufferSize));
}
