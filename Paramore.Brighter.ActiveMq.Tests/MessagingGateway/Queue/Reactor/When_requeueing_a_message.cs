using System;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using Paramore.Brighter.ActiveMq.Tests.TestDoubles;
using Paramore.Brighter.ActiveMq.Tests.Utils;
using Paramore.Brighter.JsonConverters;
using Paramore.Brighter.MessagingGateway.ActiveMq;
using Xunit;

namespace Paramore.Brighter.ActiveMq.Tests.MessagingGateway.Queue.Reactor;

[Trait("Category", "ActiveMQ")]
public class MessageProducerRequeueTestsSync : IDisposable
{
    private readonly IAmAMessageProducerSync _sender;
    private Message? _receivedMessage;
    private readonly IAmAChannelSync _channel;
    private readonly Message _message;

    public MessageProducerRequeueTestsSync()
    {
        var myCommand = new MyCommand { Value = "Test" };
        string correlationId = Uuid.NewAsString();
        var contentType = new ContentType(MediaTypeNames.Text.Plain);
        var channelName = Uuid.NewAsString();
        var routingKey = new RoutingKey(channelName);

        var subscription = new ActiveMqQueueSubscription<MyCommand>(
            subscriptionName: new SubscriptionName(channelName),
            channelName: new ChannelName(channelName),
            routingKey: routingKey,
            messagePumpType: MessagePumpType.Proactor,
            makeChannels: OnMissingChannel.Create
        );

        _message = new Message(
            new MessageHeader(myCommand.Id, routingKey, MessageType.MT_COMMAND, correlationId: correlationId, contentType: contentType),
            new MessageBody(JsonSerializer.Serialize(myCommand, JsonSerialisationOptions.Options))
        );

        var channelFactory = GatewayFactory.CreateChannel();
        _sender = GatewayFactory.CreateQueueProducer(new ActiveMqQueuePublication { Topic = routingKey });
        _channel = channelFactory.CreateSyncChannel(subscription);
    }

    [Fact]
    public void When_requeueing_a_message()
    {
        _channel.Purge(); 
        
        _sender.Send(_message);
        _receivedMessage = _channel.Receive(TimeSpan.FromSeconds(5));
        _channel.Requeue(_receivedMessage);

        var requeuedMessage = _channel.Receive(TimeSpan.FromMinutes(1));
        Assert.NotEqual(MessageType.MT_NONE, requeuedMessage.Header.MessageType);
            
        _channel.Acknowledge(requeuedMessage);
        Assert.Equal(_receivedMessage.Body.Value, requeuedMessage .Body.Value);
    }

    public void Dispose()
    {
        _sender.Dispose();
        _channel.Dispose();
    }
}
