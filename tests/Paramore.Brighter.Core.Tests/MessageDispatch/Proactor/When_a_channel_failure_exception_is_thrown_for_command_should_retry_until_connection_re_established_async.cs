﻿#region Licence
/* The MIT License (MIT)
Copyright © 2014 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Paramore.Brighter.Core.Tests.CommandProcessors.TestDoubles;
using Paramore.Brighter.Core.Tests.MessageDispatch.TestDoubles;
using Paramore.Brighter.ServiceActivator;
using Xunit;

namespace Paramore.Brighter.Core.Tests.MessageDispatch.Proactor
{
    public class MessagePumpRetryCommandOnConnectionFailureTestsAsync
    {
        private const string ChannelName = "myChannel";
        private readonly RoutingKey _routingKey = new("MyTopic");
        private readonly InternalBus _bus = new();
        private readonly FakeTimeProvider _timeProvider = new();
        private readonly IAmAMessagePump _messagePump;
        private readonly SpyCommandProcessor _commandProcessor;

        public MessagePumpRetryCommandOnConnectionFailureTestsAsync()
        {
            _commandProcessor = new SpyCommandProcessor();
            var channel = new FailingChannelAsync(
                new ChannelName(ChannelName), _routingKey, 
                new InMemoryMessageConsumer(_routingKey, _bus, _timeProvider, TimeSpan.FromMilliseconds(1000)), 
                2)
            {
                NumberOfRetries = 1
            };
            var messageMapperRegistry = new MessageMapperRegistry(
                null,
                new SimpleMessageMapperFactoryAsync(_ => new MyCommandMessageMapperAsync()));
            messageMapperRegistry.RegisterAsync<MyCommand, MyCommandMessageMapperAsync>();
            _messagePump = new Proactor<MyCommand>(_commandProcessor, messageMapperRegistry, new EmptyMessageTransformerFactoryAsync(), new InMemoryRequestContextFactory(), channel)
            {
                Channel = channel, TimeOut = TimeSpan.FromMilliseconds(500), RequeueCount = -1
            };

            var command = new MyCommand();

            //two command, will be received when subscription restored
            var message1 = new Message(
                new MessageHeader(Guid.NewGuid().ToString(), _routingKey, MessageType.MT_COMMAND), 
                new MessageBody(JsonSerializer.Serialize(command, JsonSerialisationOptions.Options))
            );
            var message2 = new Message(
                new MessageHeader(Guid.NewGuid().ToString(), _routingKey, MessageType.MT_COMMAND), 
                new MessageBody(JsonSerializer.Serialize(command, JsonSerialisationOptions.Options))
            );
            channel.Enqueue(message1);
            channel.Enqueue(message2);
            
            //end the pump
            var quitMessage = MessageFactory.CreateQuitMessage(_routingKey);
            channel.Enqueue(quitMessage);
        }

        [Fact]
        public void When_A_Channel_Failure_Exception_Is_Thrown_For_Command_Should_Retry_Until_Connection_Re_established()
        {
            _messagePump.Run();

            //_should_send_the_message_via_the_command_processor
            _commandProcessor.Commands.Count().Should().Be(2);
            _commandProcessor.Commands[0].Should().Be(CommandType.SendAsync);
            _commandProcessor.Commands[1].Should().Be(CommandType.SendAsync);
        }
    }
}
