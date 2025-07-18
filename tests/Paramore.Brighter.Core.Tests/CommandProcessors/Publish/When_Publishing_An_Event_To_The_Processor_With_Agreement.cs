﻿using System;
using System.Collections.Generic;
using Paramore.Brighter.Core.Tests.CommandProcessors.TestDoubles;
using Polly.Registry;
using Xunit;

namespace Paramore.Brighter.Core.Tests.CommandProcessors.Publish
{
    [Collection("CommandProcessor")]
    public class CommandProcessorPublishEventAgreementTests : IDisposable
    {
        private readonly CommandProcessor _commandProcessor;
        private readonly IDictionary<string, string> _receivedMessages = new Dictionary<string, string>();

        public CommandProcessorPublishEventAgreementTests()
        {
            var registry = new SubscriberRegistry();
            registry.Register<MyEvent>((request, context) =>
            {
                var myEvent = request as MyEvent;
                
                if (myEvent.Data == 4)
                    return [typeof(MyEventHandler)];
                
                return [..Array.Empty<Type>()];
            },
                [typeof(MyEventHandler)]);
            var handlerFactory = new SimpleHandlerFactorySync(_ => new MyEventHandler(_receivedMessages));

            _commandProcessor = new CommandProcessor(registry, handlerFactory, new InMemoryRequestContextFactory(), new PolicyRegistry(), new InMemorySchedulerFactory());
            PipelineBuilder<MyEvent>.ClearPipelineCache();
        }

        [Fact]
        public void When_Publishing_An_Event_To_The_Processor()
        {
            var myEvent = new MyEvent { Data = 4 };
            _commandProcessor.Publish(myEvent);

           //Should publish the command to the first event handler
           Assert.Contains(new KeyValuePair<string, string>(nameof(MyEventHandler), myEvent.Id), _receivedMessages);

        }

        public void Dispose()
        {
            CommandProcessor.ClearServiceBus();
        }
    }
}
