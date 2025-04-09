﻿using System;
using System.Text.Json;
using System.Threading.Tasks;
using Paramore.Brighter.AWS.Tests.Helpers;
using Paramore.Brighter.AWS.Tests.TestDoubles;
using Paramore.Brighter.MessagingGateway.AWSSQS;
using Xunit;

namespace Paramore.Brighter.AWS.Tests.MessagingGateway.Sqs.Standard.Proactor;

[Trait("Category", "AWS")]
public class SqsMessageProducerSendAsyncTests : IAsyncDisposable, IDisposable
{
    private readonly Message _message;
    private readonly IAmAChannelAsync _channel;
    private readonly SqsMessageProducer _messageProducer;
    private readonly ChannelFactory _channelFactory;
    private readonly MyCommand _myCommand;
    private readonly string _correlationId;
    private readonly string _replyTo;
    private readonly string _contentType;
    private readonly string _queueName;

    public SqsMessageProducerSendAsyncTests()
    {
        _myCommand = new MyCommand { Value = "Test" };
        _correlationId = Guid.NewGuid().ToString();
        _replyTo = "http:\\queueUrl";
        _contentType = "text\\plain";
        var subscriptionName = $"Producer-Send-Tests-{Guid.NewGuid().ToString()}".Truncate(45);
        _queueName = $"Producer-Send-Tests-{Guid.NewGuid().ToString()}".Truncate(45);
        var routingKey = new RoutingKey(_queueName);
        var channelName = new ChannelName(_queueName);

        var subscription = new SqsSubscription<MyCommand>(
            subscriptionName: new SubscriptionName(subscriptionName),
            channelName: channelName,
            channelType: ChannelType.PointToPoint, 
            messagePumpType: MessagePumpType.Proactor,
            makeChannels: OnMissingChannel.Create);

        _message = new Message(
            new MessageHeader(_myCommand.Id, routingKey, MessageType.MT_COMMAND, correlationId: _correlationId,
                replyTo: new RoutingKey(_replyTo), contentType: _contentType),
            new MessageBody(JsonSerializer.Serialize((object)_myCommand, JsonSerialisationOptions.Options))
        );

        var awsConnection = GatewayFactory.CreateFactory();

        _channelFactory = new ChannelFactory(awsConnection);
        _channel = _channelFactory.CreateAsyncChannel(subscription);

        _messageProducer = new SqsMessageProducer(awsConnection, new SqsPublication
        {
            ChannelName = channelName,
            MakeChannels = OnMissingChannel.Create
        });
    }

    [Fact]
    public async Task When_posting_a_message_via_the_producer_async()
    {
        // arrange
        _message.Header.Subject = "test subject";
        await _messageProducer.SendAsync(_message);

        await Task.Delay(1000);

        var message = await _channel.ReceiveAsync(TimeSpan.FromMilliseconds(5000));

        // clear the queue
        await _channel.AcknowledgeAsync(message);

        // should_send_the_message_to_aws_sqs
        Assert.Equal(MessageType.MT_COMMAND, message.Header.MessageType);

        Assert.Equal(_myCommand.Id, message.Id);
        Assert.False(message.Redelivered);
        Assert.Equal(_myCommand.Id, message.Header.MessageId);
        Assert.Contains(_queueName, message.Header.Topic.Value);
        Assert.Equal(_correlationId, message.Header.CorrelationId);
        Assert.Equal(_replyTo, message.Header.ReplyTo);
        Assert.Equal(_contentType, message.Header.ContentType);
        Assert.Equal(0, message.Header.HandledCount);
        Assert.Equal(_message.Header.Subject, message.Header.Subject);
        // allow for clock drift in the following test, more important to have a contemporary timestamp than anything
        Assert.True((message.Header.TimeStamp) > (RoundToSeconds(DateTime.UtcNow.AddMinutes(-1))));
        Assert.Equal(TimeSpan.Zero, message.Header.Delayed);
        // {"Id":"cd581ced-c066-4322-aeaf-d40944de8edd","Value":"Test","WasCancelled":false,"TaskCompleted":false}
        Assert.Equal(_message.Body.Value, message.Body.Value);
    }
        
    public void Dispose()
    {
        //Clean up resources that we have created
        _channelFactory.DeleteTopicAsync().Wait();
        _channelFactory.DeleteQueueAsync().Wait();
        _messageProducer.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _channelFactory.DeleteTopicAsync();
        await _channelFactory.DeleteQueueAsync();
        await _messageProducer.DisposeAsync();
    }

    private static DateTime RoundToSeconds(DateTime dateTime)
    {
        return new DateTime(dateTime.Ticks - (dateTime.Ticks % TimeSpan.TicksPerSecond), dateTime.Kind);
    }
}
