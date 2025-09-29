using System;
using System.Threading.Tasks;
using Paramore.Brighter.ActiveMq.Tests.TestDoubles;
using Paramore.Brighter.ActiveMq.Tests.Utils;
using Paramore.Brighter.MessagingGateway.ActiveMq;
using Xunit;

namespace Paramore.Brighter.ActiveMq.Tests.MessagingGateway.Queue.Proactor;

[Trait("Category", "ActiveMQ")]
public class ActiveMqBufferedConsumerCloudEventsTestsAsync : IDisposable 
{
    private readonly IAmAMessageProducerAsync _messageProducer;
    private readonly IAmAMessageConsumerAsync _messageConsumer;
    private readonly RoutingKey _routingKey = new(Uuid.NewAsString());

    public ActiveMqBufferedConsumerCloudEventsTestsAsync()
    {
        var publication = new ActiveMqQueuePublication
        {
            Topic = _routingKey
        };

        _messageProducer = GatewayFactory.CreateProducer(publication);
        _messageConsumer = GatewayFactory.CreateConsumer(
            new ActiveMqQueueSubscription("sub-name", 
                _routingKey.Value,
                _routingKey, 
                requestType: typeof(MyCommand),
                messagePumpType: MessagePumpType.Proactor));
    }

    [Fact]
    public async Task When_uses_cloud_events_async()
    {
        //Post one more than batch size messages
        var messageOne = new Message(
            new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND)
            {
                Type = new CloudEventsType($"Type{Guid.NewGuid():N}"),
                Subject = $"Subject{Guid.NewGuid():N}",
                Source = new Uri($"/component/{Guid.NewGuid()}", UriKind.RelativeOrAbsolute),
                DataSchema = new Uri("https://example.com/storage/tenant/container", UriKind.RelativeOrAbsolute)
            }, new MessageBody("test content One"));
        await _messageProducer.SendAsync(messageOne);

        //let them arrive
        await Task.Delay(5000);

        //Now retrieve messages from the consumer
        var messages = await _messageConsumer.ReceiveAsync(TimeSpan.FromSeconds(10));

        //We should only have three messages
        Assert.Single(messages);

        Assert.NotEqual(MessageType.MT_NONE, messages[0].Header.MessageType);
        Assert.Equal(messageOne.Header.MessageId, messages[0].Header.MessageId);
        Assert.Equal(messageOne.Header.Subject, messages[0].Header.Subject);
        Assert.Equal(messageOne.Header.Type, messages[0].Header.Type);
        Assert.Equal(messageOne.Header.Source, messages[0].Header.Source);
        Assert.Equal(messageOne.Header.DataSchema, messages[0].Header.DataSchema);
    }

    public void Dispose()
    {
        _messageConsumer.PurgeAsync().GetAwaiter().GetResult();
        _messageConsumer.DisposeAsync().GetAwaiter().GetResult();
        _messageProducer.DisposeAsync().GetAwaiter().GetResult();
    }
}
