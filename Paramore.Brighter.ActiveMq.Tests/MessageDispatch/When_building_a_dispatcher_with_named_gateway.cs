using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Brighter.ActiveMq.Tests.TestDoubles;
using Paramore.Brighter.ActiveMq.Tests.Utils;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.MessagingGateway.ActiveMq;
using Paramore.Brighter.Observability;
using Paramore.Brighter.ServiceActivator;
using Xunit;

namespace Paramore.Brighter.ActiveMq.Tests.MessageDispatch;

[Collection("CommandProcessor")]
public class DispatchBuilderWithNamedGateway : IDisposable
{
    private readonly IAmADispatchBuilder _builder;
    private Dispatcher? _dispatcher;

    public DispatchBuilderWithNamedGateway()
    {
        var messageMapperRegistry = new MessageMapperRegistry(
            new SimpleMessageMapperFactory((_) => new MyEventMessageMapper()),
            null
        );
        messageMapperRegistry.Register<MyEvent, MyEventMessageMapper>();

        var connection = GatewayFactory.CreateConnection(); 

        var container = new ServiceCollection();
        var tracer = new BrighterTracer(TimeProvider.System);
        var instrumentationOptions = InstrumentationOptions.All;
            
        var commandProcessor = CommandProcessorBuilder.StartNew()
            .Handlers(new HandlerConfiguration(new SubscriberRegistry(), new ServiceProviderHandlerFactory(container.BuildServiceProvider())))
            .DefaultResilience()
            .NoExternalBus()
            .ConfigureInstrumentation(tracer, instrumentationOptions)
            .RequestContextFactory(new InMemoryRequestContextFactory())
            .RequestSchedulerFactory(new InMemorySchedulerFactory())
            .Build();

        _builder = DispatchBuilder.StartNew()
            .CommandProcessor(commandProcessor,
                new InMemoryRequestContextFactory()
            )
            .MessageMappers(messageMapperRegistry, null, new EmptyMessageTransformerFactory(), null)
            .ChannelFactory(new ActiveMqChannelFactory(connection))
            .Subscriptions([
                new ActiveMqQueueSubscription<MyEvent>(
                    new SubscriptionName("foo"),
                    new ChannelName("mary"),
                    new RoutingKey("bt_named_gateway_dispatch"),
                    messagePumpType: MessagePumpType.Reactor,
                    timeOut: TimeSpan.FromMilliseconds(200)),
                new ActiveMqQueueSubscription<MyEvent>(
                    new SubscriptionName("bar"),
                    new ChannelName("alice"),
                    new RoutingKey("bt_named_gateway_dispatch"),
                    messagePumpType: MessagePumpType.Reactor,
                    timeOut: TimeSpan.FromMilliseconds(200))
            ])
            .ConfigureInstrumentation(tracer, instrumentationOptions);
    }

    [Fact]
    public void When_building_a_dispatcher_with_named_gateway()
    {
        _dispatcher = _builder.Build();
        Assert.NotNull(_dispatcher);
    }

    public void Dispose()
    {
        CommandProcessor.ClearServiceBus();
    }
}
