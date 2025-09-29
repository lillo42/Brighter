using System;
using System.Threading;
using Paramore.Brighter.MessagingGateway.Oracle;
using Paramore.Brighter.Oracle.Tests.TestDoubles;
using Paramore.Brighter.Oracle.Tests.Utils;
using Xunit;

namespace Paramore.Brighter.Oracle.Tests.MessagingGateway.Reactor;

[Trait("Category", "Oracle")]
public class OracleBufferedConsumerTests : IDisposable
{
    private readonly IAmAMessageProducerSync _messageProducer;
    private readonly IAmAChannelSync _channel;
    private readonly ChannelName _channelName = new($"BufferTopic{Uuid.New():N}");
    private readonly RoutingKey _routingKey = new($"BufferTopic{Uuid.New():N}");
    private const int BatchSize = 3;

    public OracleBufferedConsumerTests()
    {
        _channel = GatewayFactory.CreateChannel(new OracleAdvanceQueueSubscription<MyCommand>(
            subscriptionName: "subscription-name",
            channelName: _channelName,
            routingKey: _routingKey));

        _messageProducer =
            GatewayFactory.CreateMessageProducer(new OracleAdvanceQueuePublication<MyCommand> { Topic = _routingKey });
    }

    [Fact]
    public void When_a_message_consumer_reads_multiple_messages()
    {
        //Post one more than batch size messages
        var messageOne = new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content One"));
        _messageProducer.Send(messageOne);
        var messageTwo= new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content Two"));
        _messageProducer.Send(messageTwo);
        var messageThree= new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content Three"));
        _messageProducer.Send(messageThree);
        var messageFour= new Message(new MessageHeader(Id.Random(), _routingKey, MessageType.MT_COMMAND), new MessageBody("test content Four"));
        _messageProducer.Send(messageFour);
            
        for (var i = 0; i < BatchSize; i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            var message = _channel.Receive(TimeSpan.FromSeconds(10));
            Assert.Equal(MessageType.MT_COMMAND, message.Header.MessageType);
            
        }
    }

    public void Dispose()
    {
        _channel.Dispose();
        _messageProducer.Dispose();
    }
}
