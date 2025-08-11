using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.MessagingGateway.Oracle;
using Paramore.Brighter.Observability;
using Paramore.Brighter.Oracle.Tests.TestDoubles;
using Paramore.Brighter.Oracle.Tests.Utils;
using Paramore.Brighter.ServiceActivator;
using Xunit;

namespace Paramore.Brighter.Oracle.Tests.MessageDispatch;

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
            .ChannelFactory(new OracleChannelFactory(connection))
            .Subscriptions([
                new OracleAdvanceQueueSubscription<MyEvent>(
                   new SubscriptionName($"FOO{Uuid.New():N}"),
                    new ChannelName($"MARY{Uuid.New():N}"),
                    new RoutingKey(Uuid.New().ToString("N")),
                    messagePumpType: MessagePumpType.Reactor,
                    timeOut: TimeSpan.FromMilliseconds(200)),
                new OracleAdvanceQueueSubscription<MyEvent>(
                   new SubscriptionName($"BAR{Uuid.New():N}"),
                    new ChannelName($"ALICE{Uuid.New():N}"),
                    new RoutingKey(Uuid.New().ToString("N")),
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
