﻿using System;
using FluentAssertions;
using Paramore.Brighter.Outbox.MongoDb;
using Xunit;

namespace Paramore.Brighter.MongoDbTests.Outbox;

[Trait("Category", "MongoDb")]
public class MongoDbArchiveFetchTests : IDisposable
{
    private readonly string _collection;
    private readonly Message _messageEarliest;
    private readonly Message _messageDispatched;
    private readonly Message _messageUnDispatched;
    private readonly MongoDbOutbox _outbox;

    public MongoDbArchiveFetchTests()
    {
        _collection = $"outbox-{Guid.NewGuid():N}";
        _outbox = new MongoDbOutbox(Configuration.Create(_collection));
        var routingKey = new RoutingKey("test_topic");

        _messageEarliest = new Message(
            new MessageHeader(Guid.NewGuid().ToString(), routingKey, MessageType.MT_DOCUMENT),
            new MessageBody("message body"));
        _messageDispatched = new Message(
            new MessageHeader(Guid.NewGuid().ToString(), routingKey, MessageType.MT_DOCUMENT),
            new MessageBody("message body"));
        _messageUnDispatched = new Message(
            new MessageHeader(Guid.NewGuid().ToString(), routingKey, MessageType.MT_DOCUMENT),
            new MessageBody("message body"));
    }

    [Fact]
    public void When_Retrieving_Messages_To_Archive_UsingTimeSpan()
    {
        var context = new RequestContext();
        _outbox.Add([_messageEarliest, _messageDispatched, _messageUnDispatched], context);
        _outbox.MarkDispatched(_messageEarliest.Id, context, DateTime.UtcNow.AddHours(-3));
        _outbox.MarkDispatched(_messageDispatched.Id, context);

        var allDispatched = _outbox.DispatchedMessages(TimeSpan.Zero, context);
        var messagesOverAnHour = _outbox.DispatchedMessages(TimeSpan.FromHours(2), context);
        var messagesOver4Hours = _outbox.DispatchedMessages(TimeSpan.FromHours(4), context);

        //Assert
        allDispatched.Should().HaveCount(2);
        messagesOverAnHour.Should().ContainSingle();
        messagesOver4Hours.Should().BeEmpty();
    }
    
    public void Dispose()
    {
        Configuration.Cleanup(_collection);
    }
}
