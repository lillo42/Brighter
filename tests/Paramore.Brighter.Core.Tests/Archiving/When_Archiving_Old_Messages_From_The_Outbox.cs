﻿using System;
using System.Collections.Generic;
using System.Transactions;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Paramore.Brighter.Core.Tests.CommandProcessors.TestDoubles;
using Polly;
using Polly.Registry;
using Xunit;

namespace Paramore.Brighter.Core.Tests.Archiving;

public class ServiceBusMessageStoreArchiverTests 
{
    private readonly InMemoryOutbox _outbox;
    private readonly InMemoryArchiveProvider _archiveProvider;
    private readonly ExternalBusService<Message,CommittableTransaction> _bus;

    public ServiceBusMessageStoreArchiverTests()
    {
        const string topic = "MyTopic";

        var producer = new FakeMessageProducerWithPublishConfirmation{Publication = {Topic = new RoutingKey(topic), RequestType = typeof(MyCommand)}};

        var messageMapperRegistry = new MessageMapperRegistry(
            new SimpleMessageMapperFactory((_) => new MyCommandMessageMapper()),
            null);

        var retryPolicy = Policy
            .Handle<Exception>()
            .Retry();

        var circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreaker(1, TimeSpan.FromMilliseconds(1));

        var producerRegistry = new ProducerRegistry(new Dictionary<string, IAmAMessageProducer>
        {
            { topic, producer },
        });

        var policyRegistry = new PolicyRegistry
        {
            { CommandProcessor.RETRYPOLICY, retryPolicy },
            { CommandProcessor.CIRCUITBREAKER, circuitBreakerPolicy }
        }; 
        
        var timeProvider = new FakeTimeProvider();
        _outbox = new InMemoryOutbox(timeProvider);
        _archiveProvider = new InMemoryArchiveProvider();
        
        _bus = new ExternalBusService<Message, CommittableTransaction>(
            producerRegistry, 
            policyRegistry,
            messageMapperRegistry,
            new EmptyMessageTransformerFactory(),
            new EmptyMessageTransformerFactoryAsync(),
            _outbox,
            _archiveProvider 
        ); 
    }
    
    [Fact]
    public void When_Archiving_All_Messages_From_The_Outbox()
    {
        //arrange
        var messageOne = new Message(new MessageHeader(Guid.NewGuid().ToString(), "MyTopic", MessageType.MT_COMMAND), new MessageBody("test content"));
        _outbox.Add(messageOne);
        _outbox.MarkDispatched(messageOne.Id);
        
        var messageTwo = new Message(new MessageHeader(Guid.NewGuid().ToString(), "MyTopic", MessageType.MT_COMMAND), new MessageBody("test content"));
        _outbox.Add(messageTwo);
        _outbox.MarkDispatched(messageTwo.Id);
        
        var messageThree = new Message(new MessageHeader(Guid.NewGuid().ToString(), "MyTopic", MessageType.MT_COMMAND), new MessageBody("test content"));
        _outbox.Add(messageThree);
        _outbox.MarkDispatched(messageThree.Id);

        //act
        _outbox.EntryCount.Should().Be(3);
        
        _bus.Archive(20000);
        
        //assert
        _outbox.EntryCount.Should().Be(0);
        _archiveProvider.ArchivedMessages.Should().Contain(new KeyValuePair<string, Message>(messageOne.Id, messageOne));
        _archiveProvider.ArchivedMessages.Should().Contain(new KeyValuePair<string, Message>(messageTwo.Id, messageTwo));
        _archiveProvider.ArchivedMessages.Should().Contain(new KeyValuePair<string, Message>(messageThree.Id, messageThree));
    }
    
    [Fact]
    public void When_Archiving_Some_Messages_From_The_Outbox()
    {
        var messageOne = new Message(new MessageHeader(Guid.NewGuid().ToString(), "MyTopic", MessageType.MT_COMMAND), new MessageBody("test content"));
        _outbox.Add(messageOne);
        _outbox.MarkDispatched(messageOne.Id);
        
        var messageTwo = new Message(new MessageHeader(Guid.NewGuid().ToString(), "MyTopic", MessageType.MT_COMMAND), new MessageBody("test content"));
        _outbox.Add(messageTwo);
        _outbox.MarkDispatched(messageTwo.Id);
        
        var messageThree = new Message(new MessageHeader(Guid.NewGuid().ToString(), "MyTopic", MessageType.MT_COMMAND), new MessageBody("test content"));
        _outbox.Add(messageThree);

        //act
        _outbox.EntryCount.Should().Be(3);
        
        _bus.Archive(20000);
        
        //assert
        _outbox.EntryCount.Should().Be(1);
        _archiveProvider.ArchivedMessages.Should().Contain(new KeyValuePair<string, Message>(messageOne.Id, messageOne));
        _archiveProvider.ArchivedMessages.Should().Contain(new KeyValuePair<string, Message>(messageTwo.Id, messageTwo));
        _archiveProvider.ArchivedMessages.Should().NotContain((new KeyValuePair<string, Message>(messageThree.Id, messageThree)));
        
    }
    
    [Fact]
    public void When_Archiving_No_Messages_From_The_Outbox()
    {
        var messageOne = new Message(new MessageHeader(Guid.NewGuid().ToString(), "MyTopic", MessageType.MT_COMMAND), new MessageBody("test content"));
        _outbox.Add(messageOne);
        
        var messageTwo = new Message(new MessageHeader(Guid.NewGuid().ToString(), "MyTopic", MessageType.MT_COMMAND), new MessageBody("test content"));
        _outbox.Add(messageTwo);
        
        var messageThree = new Message(new MessageHeader(Guid.NewGuid().ToString(), "MyTopic", MessageType.MT_COMMAND), new MessageBody("test content"));
        _outbox.Add(messageThree);

        //act
        _outbox.EntryCount.Should().Be(3);
        
        _bus.Archive(20000);
        
        //assert
        _outbox.EntryCount.Should().Be(3);
        _archiveProvider.ArchivedMessages.Should().NotContain(new KeyValuePair<string, Message>(messageOne.Id, messageOne));
        _archiveProvider.ArchivedMessages.Should().NotContain(new KeyValuePair<string, Message>(messageTwo.Id, messageTwo));
        _archiveProvider.ArchivedMessages.Should().NotContain((new KeyValuePair<string, Message>(messageThree.Id, messageThree)));
    }
    
    [Fact]
    public void When_Archiving_An_Empty_The_Outbox()
    {
        _bus.Archive(20000);
        
        //assert
        _outbox.EntryCount.Should().Be(0);
    }
}
