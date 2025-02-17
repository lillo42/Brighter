using Azure.Messaging.ServiceBus;
using Paramore.Brighter.MessagingGateway.AzureServiceBus.ClientProvider;

namespace Paramore.Brighter.MessageScheduler.Azure;

/// <summary>
/// The <see cref="AzureServiceBusScheduler"/> factory.
/// </summary>
/// <param name="client"></param>
/// <param name="topic"></param>
public class AzureServiceBusSchedulerFactory(IServiceBusClientProvider client, RoutingKey topic)
    : IAmAMessageSchedulerFactory, IAmARequestSchedulerFactory
{
    /// <summary>
    /// The Azure client provider
    /// </summary>
    public IServiceBusClientProvider ClientProvider { get; set; } = client;

    /// <summary>
    /// The Sender options that the scheduler should use.
    /// </summary>
    public ServiceBusSenderOptions? SenderOptions { get; set; }

    /// <summary>
    /// The topic or queue that the azure scheduler should use
    /// </summary>
    public RoutingKey Topic { get; set; } = topic;

    /// <summary>
    /// The <see cref="System.TimeProvider"/>
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <inheritdoc />
    public IAmAMessageScheduler Create(IAmACommandProcessor processor)
    {
        return new AzureServiceBusScheduler(ClientProvider.GetServiceBusClient()
                .CreateSender(Topic, SenderOptions),
            Topic,
            TimeProvider);
    }

    /// <inheritdoc />
    public IAmARequestSchedulerSync CreateSync(IAmACommandProcessor processor)
    {
        return new AzureServiceBusScheduler(ClientProvider.GetServiceBusClient()
                .CreateSender(Topic, SenderOptions),
            Topic,
            TimeProvider);
    }

    /// <inheritdoc />
    public IAmARequestSchedulerAsync CreateAsync(IAmACommandProcessor processor)
    {
        return new AzureServiceBusScheduler(ClientProvider.GetServiceBusClient()
                .CreateSender(Topic, SenderOptions),
            Topic,
            TimeProvider);
    }
}
