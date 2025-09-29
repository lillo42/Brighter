using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Brighter.ActiveMq.Tests.TestDoubles;
using Paramore.Brighter.ActiveMq.Tests.Utils;
using Paramore.Brighter.MessagingGateway.ActiveMq;
using Xunit;

namespace Paramore.Brighter.ActiveMq.Tests.MessagingGateway.Queue.Reactor;

[Trait("Category", "ActiveMQ")]
public class ActiveMqBufferedConsumerMultipleMessagesTestsSync : IDisposable
{
    private readonly IAmAMessageProducerSync _messageProducer;
    private readonly IAmAMessageConsumerSync _messageConsumer;
    private readonly RoutingKey _routingKey = new(Guid.NewGuid().ToString());
    private const int BatchSize = 3;

    public ActiveMqBufferedConsumerMultipleMessagesTestsSync()
    {
        var publication = new ActiveMqQueuePublication
        {
            Topic = _routingKey,
        };

        _messageProducer = GatewayFactory.CreateQueueProducer(publication);
        _messageConsumer = GatewayFactory.CreateQueueConsumer(
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
    public void When_a_message_consumer_reads_multiple_messages()
    {
        //Post one more than batch size messages
        var messageOne = new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content One"));
        _messageProducer.Send(messageOne);
        var messageTwo = new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content Two"));
        _messageProducer.Send(messageTwo);
        var messageThree = new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content Three"));
        _messageProducer.Send(messageThree);
        var messageFour = new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content Four"));
        _messageProducer.Send(messageFour);

        //let them arrive
        Thread.Sleep(TimeSpan.FromSeconds(5));

        for (var i = 0; i < BatchSize; i++)
        {
            //Now retrieve messages from the consumer
            var messages = _messageConsumer.Receive(TimeSpan.FromMilliseconds(5000));
            
            Assert.Single(messages);
            Assert.NotEqual(MessageType.MT_NONE, messages[0].Header.MessageType);
            _messageConsumer.Acknowledge(messages[0]);
        }
    }

    public void Dispose()
    {
        _messageConsumer.Purge();
        _messageProducer.Dispose();
        _messageConsumer.Dispose();
    }
}
