﻿using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Brighter.AWS.Tests.Helpers;
using Paramore.Brighter.AWS.Tests.TestDoubles;
using Paramore.Brighter.MessageScheduler.Aws;
using Paramore.Brighter.MessagingGateway.AWSSQS;
using Paramore.Brighter.Scheduler.Events;
using Xunit;

namespace Paramore.Brighter.AWS.Tests.Scheduler.Messages.Sqs;

[Trait("Fragile", "CI")] // It isn't really fragile, it's time consumer (1-2 per test)
[Collection("Scheduler SQS")]
public class SqsSchedulingMessageViaFireSchedulerTest : IDisposable
{
    private const string ContentType = "text\\plain";
    private const int BufferSize = 3;
    private readonly SqsMessageProducer _messageProducer;
    private readonly SqsMessageConsumer _consumer;
    private readonly string _queueName;
    private readonly ChannelFactory _channelFactory;
    private readonly IAmAMessageSchedulerFactory _factory;

    public SqsSchedulingMessageViaFireSchedulerTest()
    {
        var awsConnection = GatewayFactory.CreateFactory();

        _channelFactory = new ChannelFactory(awsConnection);
        var subscriptionName = $"Buffered-Scheduler-Tests-{Guid.NewGuid().ToString()}".Truncate(45);
        _queueName = $"Buffered-Scheduler-Tests-{Guid.NewGuid().ToString()}".Truncate(45);

        //we need the channel to create the queues and notifications
        var routingKey = new RoutingKey(_queueName);

        var channel = _channelFactory.CreateSyncChannel(new SqsSubscription<MyCommand>(
            name: new SubscriptionName(subscriptionName),
            channelName: new ChannelName(_queueName),
            routingKey: routingKey,
            bufferSize: BufferSize,
            makeChannels: OnMissingChannel.Create,
            channelType: ChannelType.PointToPoint
        ));

        //we want to access via a consumer, to receive multiple messages - we don't want to expose on channel
        //just for the tests, so create a new consumer from the properties
        _consumer = new SqsMessageConsumer(awsConnection, channel.Name.ToValidSQSQueueName(), BufferSize);
        _messageProducer = new SqsMessageProducer(awsConnection,
            new SqsPublication { MakeChannels = OnMissingChannel.Create });

        _factory = new AwsSchedulerFactory(awsConnection, "brighter-scheduler")
        {
            UseMessageTopicAsTarget = false, 
            MakeRole = OnMissingRole.Create,
            SchedulerTopicOrQueue = routingKey
        };
    }

    [Fact]
    public void When_Scheduling_A_Sqs_Message_Via_FireScheduler()
    {
        var routingKey = new RoutingKey(_queueName);
        var message = new Message(
            new MessageHeader(Guid.NewGuid().ToString(), routingKey, MessageType.MT_COMMAND,
                correlationId: Guid.NewGuid().ToString(), contentType: ContentType),
            new MessageBody("test content one")
        );

        var scheduler = (IAmAMessageSchedulerSync)_factory.Create(null!)!;
        scheduler.Schedule(message, TimeSpan.FromMinutes(1));

        Thread.Sleep(TimeSpan.FromMinutes(1));

        var stopAt = DateTimeOffset.UtcNow.AddMinutes(2);
        while (stopAt > DateTimeOffset.UtcNow)
        {
            var messages = _consumer.Receive(TimeSpan.FromMinutes(1));
            Assert.Single(messages);

            if (messages[0].Header.MessageType != MessageType.MT_NONE)
            {
                Assert.Equal(MessageType.MT_COMMAND, messages[0].Header.MessageType);
                Assert.True(messages[0].Body.Value.Any());
                var m = JsonSerializer.Deserialize<FireAwsScheduler>(messages[0].Body.Value,
                    JsonSerialisationOptions.Options);
                Assert.NotNull(m);
                Assert.Equivalent(message, m.Message);
                Assert.False(m.Async);
                _consumer.Acknowledge(messages[0]);
                return;
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        Assert.Fail("The message wasn't fired");
    }

    public void Dispose()
    {
        _channelFactory.DeleteQueueAsync().GetAwaiter().GetResult();
        _messageProducer.Dispose();
        _consumer.Dispose();
    }
}
