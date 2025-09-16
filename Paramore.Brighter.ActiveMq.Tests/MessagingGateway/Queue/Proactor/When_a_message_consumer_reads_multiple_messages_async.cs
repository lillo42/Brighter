using System;
using System.Threading.Tasks;
using Paramore.Brighter.ActiveMq.Tests.TestDoubles;
using Paramore.Brighter.ActiveMq.Tests.Utils;
using Paramore.Brighter.MessagingGateway.ActiveMq;
using Xunit;

namespace Paramore.Brighter.ActiveMq.Tests.MessagingGateway.Queue.Proactor;

[Trait("Category", "ActiveMq")]
public class ActiveMqBufferedConsumerMultipleMessagesTestsAsync : IAsyncDisposable
{
    private readonly IAmAMessageProducerAsync _messageProducer;
    private readonly IAmAMessageConsumerAsync _messageConsumer;
    private readonly RoutingKey _routingKey = new(Guid.NewGuid().ToString());
    private const int BatchSize = 3;

    public ActiveMqBufferedConsumerMultipleMessagesTestsAsync()
    {
        var publication = new ActiveMqQueuePublication
        {
            Topic = _routingKey,
        };

        var connection = GatewayFactory.CreateConnection();
        _messageProducer = GatewayFactory.CreateProducer(connection, publication);
        _messageConsumer = GatewayFactory.CreateConsumer(
            connection,
            new ActiveMqQueueSubscription(
                "sub-name",
                _routingKey.Value,
                _routingKey,
                bufferSize: BatchSize,
                messagePumpType: MessagePumpType.Proactor,
                requestType: typeof(MyCommand)
            )
        );
    }

    [Fact]
    public async Task When_a_message_consumer_reads_multiple_messages()
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

    public async ValueTask DisposeAsync()
    {
        await _messageConsumer.PurgeAsync();
        await _messageProducer.DisposeAsync();
        await _messageConsumer.DisposeAsync();
    }
}
