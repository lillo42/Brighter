﻿using System;
using Paramore.Brighter.Core.Tests.CommandProcessors.TestDoubles;
using Paramore.Brighter.Core.Tests.TestHelpers;
using Xunit;

namespace Paramore.Brighter.Core.Tests.CommandProcessors.Pipeline
{
    public class BuildPipelineFaults
    {
        private readonly PipelineBuilder<MyCommand> _chainBuilder;
        private readonly RequestContext _requestContext;

        public BuildPipelineFaults()
        {
            var registry = new SubscriberRegistry();
            registry.Register<MyCommand, MyCommandHandler>();

            //We'll simulate an IoC error
            var handlerFactory = new SimpleHandlerFactorySync(_ => throw new InvalidOperationException("Could not create handler"));
            _requestContext = new RequestContext();

            _chainBuilder = new PipelineBuilder<MyCommand>(registry, handlerFactory);
            PipelineBuilder<MyCommand>.ClearPipelineCache();
        }

        [Fact]
        public void When_Building_A_Pipeline_Failures_Should_Be_ConfigurationErrors()
        {
            var exception = Catch.Exception(() => _chainBuilder.Build(new MyCommand(), _requestContext));
            Assert.NotNull(exception);
            Assert.IsType<ConfigurationException>(exception);
            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }
    }
}
