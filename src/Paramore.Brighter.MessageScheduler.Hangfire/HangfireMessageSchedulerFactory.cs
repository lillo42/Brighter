﻿using Hangfire;

namespace Paramore.Brighter.MessageScheduler.Hangfire;

/// <summary>
/// The <see cref="HangfireMessageScheduler"/> factory
/// </summary>
/// <param name="client">The hangfire client</param>
public class HangfireMessageSchedulerFactory(IBackgroundJobClientV2 client)
    : IAmAMessageSchedulerFactory, IAmARequestSchedulerFactory
{
    /// <summary>
    /// The Hangfire queu
    /// </summary>
    public string? Queue { get; set; }

    /// <summary>
    /// The <see cref="IBackgroundJobClientV2"/>.
    /// </summary>
    public IBackgroundJobClientV2 Client { get; set; } = client;

    /// <summary>
    /// The <see cref="System.TimeProvider"/>
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <inheritdoc />
    public IAmAMessageScheduler Create(IAmACommandProcessor processor)
        => new HangfireMessageScheduler(Client, Queue, TimeProvider);

    /// <inheritdoc />
    public IAmARequestSchedulerSync CreateSync(IAmACommandProcessor processor)
        => new HangfireMessageScheduler(Client, Queue, TimeProvider);

    /// <inheritdoc />
    public IAmARequestSchedulerAsync CreateAsync(IAmACommandProcessor processor)
        => new HangfireMessageScheduler(Client, Queue, TimeProvider);
}
