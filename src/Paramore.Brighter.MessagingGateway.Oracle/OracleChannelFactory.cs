using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Brighter.MessagingGateway.Oracle;

public class OracleChannelFactory(OracleMessagingGatewayConnection connection) : IAmAChannelFactory
{
    private readonly OracleMessageConsumerFactory _factory = new(connection);
    
    /// <inheritdoc />
    public IAmAChannelSync CreateSyncChannel(Subscription subscription) 
        => new Channel(subscription.ChannelName, subscription.RoutingKey,
            _factory.Create(subscription), subscription.BufferSize);

    /// <inheritdoc />
    public IAmAChannelAsync CreateAsyncChannel(Subscription subscription)
        => new ChannelAsync(subscription.ChannelName, subscription.RoutingKey,
            _factory.CreateAsync(subscription), subscription.BufferSize);

    /// <inheritdoc />
    public Task<IAmAChannelAsync> CreateAsyncChannelAsync(Subscription subscription, CancellationToken ct = default)
        => Task.FromResult<IAmAChannelAsync>(new ChannelAsync(subscription.ChannelName, subscription.RoutingKey,
            _factory.CreateAsync(subscription), subscription.BufferSize));
}
