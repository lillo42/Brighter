using System;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Paramore.Brighter.ActiveMq.Tests.TestDoubles;
using Paramore.Brighter.ActiveMq.Tests.Utils;
using Paramore.Brighter.JsonConverters;
using Paramore.Brighter.MessagingGateway.ActiveMq;
using Xunit;

namespace Paramore.Brighter.ActiveMq.Tests.MessagingGateway.Queue.Proactor;

[Trait("Category", "ActiveMQ")]
public class MessageProducerRequeueTestsAsync : IDisposable
{
    private readonly IAmAMessageProducerAsync _sender;
    private readonly IAmAChannelAsync _channel;
    private readonly Message _message;

    public MessageProducerRequeueTestsAsync()
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
        _sender = GatewayFactory.CreateProducer(new ActiveMqQueuePublication { Topic = routingKey });
        _channel = channelFactory.CreateAsyncChannel(subscription);
    }

    [Fact]
    public async Task When_requeueing_a_message_async()
    {
        await _channel.PurgeAsync();
        
        await _sender.SendAsync(_message);
        var receivedMessage = await _channel.ReceiveAsync(TimeSpan.FromSeconds(5));
        await _channel.RequeueAsync(receivedMessage);

        var requeuedMessage = await _channel.ReceiveAsync(TimeSpan.FromMinutes(1));
        Assert.NotEqual(MessageType.MT_NONE, requeuedMessage.Header.MessageType);
            
        await _channel.AcknowledgeAsync(requeuedMessage);
        Assert.Equal(receivedMessage.Body.Value, requeuedMessage.Body.Value);
    }

    public void Dispose()
    {
        _sender.DisposeAsync().GetAwaiter().GetResult();
        _channel.Dispose();
    }
}
