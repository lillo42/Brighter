using System;
using System.Linq;
using System.Threading.Tasks;
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
public class DispatchBuilderTestsAsync : IDisposable
{
    private readonly IAmADispatchBuilder _builder;
    private Dispatcher? _dispatcher;

    public DispatchBuilderTestsAsync()
    {
        var messageMapperRegistry = new MessageMapperRegistry(
            null,
            new SimpleMessageMapperFactoryAsync(_ => new MyEventMessageMapperAsync()));
        messageMapperRegistry.RegisterAsync<MyEvent, MyEventMessageMapperAsync>();

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
            .MessageMappers(null!, messageMapperRegistry, null, new EmptyMessageTransformerFactoryAsync())
            .ChannelFactory(new OracleChannelFactory(connection))
            .Subscriptions([
                new OracleAdvanceQueueSubscription<MyEvent>(
                    new SubscriptionName($"FOO{Uuid.New():N}"),
                    new ChannelName($"MARY{Uuid.New():N}"),
                    new RoutingKey(Uuid.New().ToString("N")),
                    messagePumpType: MessagePumpType.Proactor,
                    timeOut: TimeSpan.FromMilliseconds(200)),
                new OracleAdvanceQueueSubscription<MyEvent>(
                    new SubscriptionName($"BAR{Uuid.New():N}"),
                    new ChannelName($"ALICE{Uuid.New():N}"),
                    new RoutingKey(Uuid.New().ToString("N")),
                    messagePumpType: MessagePumpType.Proactor,
                    timeOut: TimeSpan.FromMilliseconds(200))
            ])
            .ConfigureInstrumentation(tracer, instrumentationOptions);
    }
                
    [Fact]
    public async Task When_Building_A_Dispatcher_With_Async()
    {
        _dispatcher = _builder.Build();

        Assert.NotNull(_dispatcher);
        Assert.NotNull(GetConnection("foo"));
        Assert.NotNull(GetConnection("bar"));
        
        Assert.Equal(DispatcherState.DS_AWAITING, _dispatcher.State);

        await Task.Delay(1000);

        _dispatcher.Receive();

        await Task.Delay(1000);

        Assert.Equal(DispatcherState.DS_RUNNING, _dispatcher.State);

        await _dispatcher.End();
    }

    public void Dispose()
    {
        CommandProcessor.ClearServiceBus();
    }

    private Subscription? GetConnection(string name)
    {
        return _dispatcher!.Subscriptions.SingleOrDefault(conn => conn.Name == name);
    }
}
