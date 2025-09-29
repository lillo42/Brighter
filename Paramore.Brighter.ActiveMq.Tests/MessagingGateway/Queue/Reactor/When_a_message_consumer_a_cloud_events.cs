using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Brighter.ActiveMq.Tests.TestDoubles;
using Paramore.Brighter.ActiveMq.Tests.Utils;
using Paramore.Brighter.MessagingGateway.ActiveMq;
using Xunit;

namespace Paramore.Brighter.ActiveMq.Tests.MessagingGateway.Queue.Reactor;

[Trait("Category", "ActiveMQ")]
public class ActiveMqBufferedConsumerCloudEventsTestsSync : IDisposable 
{
    private readonly IAmAMessageProducerSync _messageProducer;
    private readonly IAmAMessageConsumerSync _messageConsumer;
    private readonly RoutingKey _routingKey = new(Uuid.NewAsString());

    public ActiveMqBufferedConsumerCloudEventsTestsSync()
    {
        var publication = new ActiveMqQueuePublication
        {
            Topic = _routingKey
        };

        _messageProducer = GatewayFactory.CreateQueueProducer(publication);
        _messageConsumer = GatewayFactory.CreateQueueConsumer(
            new ActiveMqQueueSubscription("sub-name", 
                _routingKey.Value,
                _routingKey, 
                requestType: typeof(MyCommand),
                messagePumpType: MessagePumpType.Proactor));
    }

    [Fact]
    public void When_uses_cloud_events()
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
        _messageProducer.Send(messageOne);

        //let them arrive
        Thread.Sleep(TimeSpan.FromSeconds(1));

        //Now retrieve messages from the consumer
        var messages = _messageConsumer.Receive(TimeSpan.FromSeconds(10));

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
        _messageConsumer.Purge();
        _messageConsumer.Dispose();
        _messageProducer.Dispose();
    }
}
