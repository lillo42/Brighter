﻿using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Paramore.Brighter.Redis.Tests.MessagingGateway;

[Collection("Redis Shared Pool")]   //shared connection pool so run sequentially
[Trait("Category", "Redis")]
public class RedisRequeueWithDelayTestsAsync : IClassFixture<RedisFixture>
{
    private readonly RedisFixture _redisFixture;
    private readonly Message _messageOne;

    public RedisRequeueWithDelayTestsAsync(RedisFixture redisFixture)
    {
        const string topic = "test";
        _redisFixture = redisFixture;
        _messageOne = new Message(
            new MessageHeader(Guid.NewGuid().ToString(), new RoutingKey(topic), MessageType.MT_COMMAND),
            new MessageBody("test content")
        );
    }

    [Fact]
    public async Task When_requeing_a_failed_message_with_delay_async()
    {
        // Clear the queue, and ensure it exists
        await _redisFixture.MessageConsumer.ReceiveAsync(TimeSpan.FromMilliseconds(1000));

        // Send & receive a message
        await _redisFixture.MessageProducer.SendAsync(_messageOne);
        var message = (await _redisFixture.MessageConsumer.ReceiveAsync(TimeSpan.FromMilliseconds(1000))).Single();
        message.Header.HandledCount.Should().Be(0);
        message.Header.Delayed.Should().Be(TimeSpan.Zero);

        // Now requeue with a delay
        await _redisFixture.MessageConsumer.RequeueAsync(_messageOne, TimeSpan.FromMilliseconds(1000));

        // Receive and assert
        message = (await _redisFixture.MessageConsumer.ReceiveAsync(TimeSpan.FromMilliseconds(1000))).Single();
        message.Header.HandledCount.Should().Be(1);
        message.Header.Delayed.Should().Be(TimeSpan.FromMilliseconds(1000));
    }
}