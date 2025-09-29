using System;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Brighter.ActiveMq.Tests.TestDoubles;
using Paramore.Brighter.ActiveMq.Tests.Utils;
using Paramore.Brighter.JsonConverters;
using Paramore.Brighter.MessagingGateway.ActiveMq;
using Xunit;

namespace Paramore.Brighter.ActiveMq.Tests.MessagingGateway.Queue.Reactor;

[Trait("Category", "ActiveMQ")]
public class MessageProducerSendSyncTests : IDisposable 
{
    private readonly Message _message;
    private readonly IAmAChannelSync _channel;
    private readonly IAmAMessageProducerSync _messageProducer;
    private readonly MyCommand _myCommand;
    private readonly Id _correlationId;

    public MessageProducerSendSyncTests()
    {
        _myCommand = new MyCommand { Value = "Test" };
        _correlationId = Id.Random();
        var contentType = new ContentType(MediaTypeNames.Text.Plain);
        var channelName = Uuid.NewAsString();
        var publication = new ActiveMqQueuePublication<MyCommand> { Topic = channelName };

        var mqSubscription = new ActiveMqQueueSubscription<MyCommand>(
            subscriptionName: new SubscriptionName(channelName),
            channelName: new ChannelName(channelName),
            routingKey: publication.Topic!,
            messagePumpType: MessagePumpType.Proactor
        );

        _message = new Message(
            new MessageHeader(_myCommand.Id, publication.Topic!, MessageType.MT_COMMAND, correlationId: _correlationId, contentType: contentType),
            new MessageBody(JsonSerializer.Serialize(_myCommand, JsonSerialisationOptions.Options)));

        var channelFactory = GatewayFactory.CreateChannel(); 
        
        _channel = channelFactory.CreateSyncChannel(mqSubscription);
        _messageProducer = GatewayFactory.CreateQueueProducer(publication);
    }

    [Fact]
    public void When_posting_a_message_via_the_producer()
    {
        // arrange
        _channel.Purge();
        
        _message.Header.Subject = "test subject";
        _messageProducer.Send(_message);

        Thread.Sleep(TimeSpan.FromSeconds(1));

        var message = _channel.Receive(TimeSpan.FromMilliseconds(5000));

        // clear the queue
        _channel.Acknowledge(message);

        // should_send_the_message_to_aws_sqs
        Assert.Equal(MessageType.MT_COMMAND, message.Header.MessageType);

        Assert.Equal(_myCommand.Id, message.Id);
        Assert.False(message.Redelivered);
        Assert.Equal(_myCommand.Id, message.Header.MessageId);
        Assert.Contains(_messageProducer.Publication.Topic!.Value, message.Header.Topic.Value);
        Assert.Equal(_correlationId, message.Header.CorrelationId);
        Assert.Equal(0, message.Header.HandledCount);
        Assert.Equal(_message.Header.Subject, message.Header.Subject);
        // allow for clock drift in the following test, more important to have a contemporary timestamp than anything
        Assert.True((message.Header.TimeStamp) > (RoundToSeconds(DateTime.UtcNow.AddMinutes(-1))));
        Assert.Equal(TimeSpan.Zero, message.Header.Delayed);
        // {"Id":"cd581ced-c066-4322-aeaf-d40944de8edd","Value":"Test","WasCancelled":false,"TaskCompleted":false}
        Assert.Equal(_message.Body.Value, message.Body.Value);
    }
    
    private static DateTime RoundToSeconds(DateTime dateTime)
    {
        return new DateTime(dateTime.Ticks - (dateTime.Ticks % TimeSpan.TicksPerSecond), dateTime.Kind);
    }

    public void Dispose()
    {
        _messageProducer.Dispose();
        _channel.Dispose();
    }
}
