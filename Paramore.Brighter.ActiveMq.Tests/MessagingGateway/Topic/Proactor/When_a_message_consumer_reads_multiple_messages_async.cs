using System;
using System.Threading.Tasks;
using Paramore.Brighter.ActiveMq.Tests.TestDoubles;
using Paramore.Brighter.ActiveMq.Tests.Utils;
using Paramore.Brighter.MessagingGateway.ActiveMq;
using Xunit;

namespace Paramore.Brighter.ActiveMq.Tests.MessagingGateway.Topic.Proactor;

[Trait("Category", "ActiveMQ")]
public class ActiveMqBufferedConsumerMultipleMessagesTestsAsync : IDisposable
{
    private readonly IAmAMessageProducerAsync _messageProducer;
    private readonly IAmAMessageConsumerAsync _messageConsumer;
    private readonly RoutingKey _routingKey = new(Guid.NewGuid().ToString());
    private const int BatchSize = 3;

    public ActiveMqBufferedConsumerMultipleMessagesTestsAsync()
    {
        var publication = new ActiveMqTopicPublication
        {
            Topic = _routingKey,
        };

        _messageProducer = GatewayFactory.CreateProducer(publication);
        _messageConsumer = GatewayFactory.CreateConsumer(
            new ActiveMqTopicSubscription(
                "sub-name",
                Uuid.NewAsString(),
                _routingKey,
                bufferSize: BatchSize,
                messagePumpType: MessagePumpType.Proactor,
                requestType: typeof(MyCommand)
            )
        );
    }

    [Fact]
    public async Task When_a_message_consumer_reads_multiple_messages_async()
    {
        //Post one more than batch size messages
        var messageOne = new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content One"));
        await _messageProducer.SendAsync(messageOne);
        var messageTwo = new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content Two"));
        await _messageProducer.SendAsync(messageTwo);
        var messageThree = new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content Three"));
        await _messageProducer.SendAsync(messageThree);
        var messageFour = new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content Four"));
        await _messageProducer.SendAsync(messageFour);

        //let them arrive
        await Task.Delay(1000);

        for (var i = 0; i < BatchSize; i++)
        {
            //Now retrieve messages from the consumer
            var messages = await _messageConsumer.ReceiveAsync(TimeSpan.FromMilliseconds(5000));
            
            Assert.Single(messages);
            Assert.NotEqual(MessageType.MT_NONE, messages[0].Header.MessageType);
            await _messageConsumer.AcknowledgeAsync(messages[0]);
        }
    }

    public void Dispose()
    {
        _messageConsumer.PurgeAsync().GetAwaiter().GetResult();
        _messageProducer.DisposeAsync().GetAwaiter().GetResult();
        _messageConsumer.DisposeAsync().GetAwaiter().GetResult();
    }
}
