using System;
using System.Linq;
using System.Threading;
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
public class DispatchBuilderTests : IDisposable
{
    private readonly IAmADispatchBuilder _builder;
    private readonly string _subscription1;
    private readonly string _subscription2;
    private Dispatcher? _dispatcher;

    public DispatchBuilderTests()
    {
        var messageMapperRegistry = new MessageMapperRegistry(
            new SimpleMessageMapperFactory(_ => new MyEventMessageMapper()),
            null);
        messageMapperRegistry.Register<MyEvent, MyEventMessageMapper>();

        var connection = GatewayFactory.CreateConnection(); 
        var container = new ServiceCollection();

        var tracer = new BrighterTracer(TimeProvider.System);
        const InstrumentationOptions instrumentationOptions = InstrumentationOptions.All;
            
        var commandProcessor = CommandProcessorBuilder.StartNew()
            .Handlers(new HandlerConfiguration(new SubscriberRegistry(), new ServiceProviderHandlerFactory(container.BuildServiceProvider())))
            .DefaultResilience()
            .NoExternalBus()
            .ConfigureInstrumentation(tracer, instrumentationOptions)
            .RequestContextFactory(new InMemoryRequestContextFactory())
            .RequestSchedulerFactory(new InMemorySchedulerFactory())
            .Build();

        _subscription1 = Uuid.NewAsString();
        _subscription2 = Uuid.NewAsString();

        _builder = DispatchBuilder.StartNew()
            .CommandProcessor(commandProcessor,
                new InMemoryRequestContextFactory()
            )
            .MessageMappers(messageMapperRegistry, null, null, null)
            .ChannelFactory(new ActiveMqChannelFactory(connection))
            .Subscriptions([
                new ActiveMqQueueSubscription<MyEvent>(
                    new SubscriptionName(_subscription1),
                    new ChannelName(Uuid.NewAsString()),
                    new RoutingKey(Uuid.NewAsString()),
                    messagePumpType: MessagePumpType.Reactor,
                    timeOut: TimeSpan.FromMilliseconds(200)),
                new ActiveMqQueueSubscription<MyEvent>(
                    new SubscriptionName(_subscription2),
                    new ChannelName(Uuid.NewAsString()),
                    new RoutingKey(Uuid.NewAsString()),
                    messagePumpType: MessagePumpType.Reactor,
                    timeOut: TimeSpan.FromMilliseconds(200))
            ])
            .ConfigureInstrumentation(tracer);
    }

    [Fact]
    public void When_Building_A_Dispatcher()
    {
        _dispatcher = _builder.Build();

        Assert.NotNull(_dispatcher);
        Assert.NotNull(GetConnection(_subscription1));
        Assert.NotNull(GetConnection(_subscription2));
        Assert.Equal(DispatcherState.DS_AWAITING, _dispatcher.State);
            
        Thread.Sleep(1000);

        _dispatcher.Receive();

        Thread.Sleep(1000);

        Assert.Equal(DispatcherState.DS_RUNNING, _dispatcher.State);

        _dispatcher.End().GetAwaiter().GetResult();
            
        Assert.Equal(DispatcherState.DS_STOPPED, _dispatcher.State);
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
