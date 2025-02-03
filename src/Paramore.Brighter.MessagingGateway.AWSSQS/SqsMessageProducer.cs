﻿#region Licence

/* The MIT License (MIT)
Copyright © 2022 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Brighter.Tasks;

namespace Paramore.Brighter.MessagingGateway.AWSSQS;

/// <summary>
/// The SQS Message producer
/// </summary>
public class SqsMessageProducer : AWSMessagingGateway, IAmAMessageProducerAsync, IAmAMessageProducerSync
{
    private readonly SqsPublication _publication;
    private readonly AWSClientFactory _clientFactory;

    /// <summary>
    /// The publication configuration for this producer
    /// </summary>
    public Publication Publication => _publication;

    /// <summary>
    /// The OTel Span we are writing Producer events too
    /// </summary>
    public Activity? Span { get; set; }

    /// <inheritdoc />
    public IAmAMessageScheduler? Scheduler { get; set; }

    /// <summary>
    /// Initialize a new instance of the <see cref="SqsMessageProducer"/>.
    /// </summary>
    /// <param name="connection">How do we connect to AWS in order to manage middleware</param>
    /// <param name="publication">Configuration of a producer</param>
    public SqsMessageProducer(AWSMessagingGatewayConnection connection, SqsPublication publication)
        : base(connection)
    {
        _publication = publication;
        _clientFactory = new AWSClientFactory(connection);

        if (publication.QueueUrl != null)
        {
            ChannelQueueUrl = publication.QueueUrl;
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public ValueTask DisposeAsync() => new();

    /// <summary>
    /// Confirm the queue exists.
    /// </summary>
    /// <param name="queue">The queue name.</param>
    public bool ConfirmQueueExists(string? queue = null)
        => BrighterAsyncContext.Run(async () => await ConfirmQueueExistsAsync(queue));

    /// <summary>
    /// Confirm the queue exists.
    /// </summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Return true if the queue exists otherwise return false</returns>
    public async Task<bool> ConfirmQueueExistsAsync(string? queue = null, CancellationToken cancellationToken = default)
    {
        //Only do this on first send for a topic for efficiency; won't auto-recreate when goes missing at runtime as a result
        if (!string.IsNullOrEmpty(ChannelQueueUrl))
        {
            return true;
        }

        _publication.SqsAttributes ??= new SqsAttributes();

        // For SQS Publish, it should be always Point-to-Point
        _publication.SqsAttributes.ChannelType = ChannelType.PointToPoint;

        RoutingKey? routingKey = null;
        if (queue is not null)
        {
            routingKey = new RoutingKey(queue);
        }
        else if (_publication.Topic is not null)
        {
            routingKey = _publication.Topic;
        }

        if (RoutingKey.IsNullOrEmpty(routingKey))
        {
            throw new ConfigurationException("No topic specified for producer");
        }

        var queueUrl = await EnsureQueueAsync(
            routingKey,
            _publication.FindQueueBy,
            _publication.SqsAttributes,
            _publication.MakeChannels,
            cancellationToken);

        return !string.IsNullOrEmpty(queueUrl);
    }

    public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
        => await SendWithDelayAsync(message, TimeSpan.Zero, cancellationToken);

    public async Task SendWithDelayAsync(Message message, TimeSpan? delay,
        CancellationToken cancellationToken = default)
    {
        delay ??= TimeSpan.Zero;
        if (delay > TimeSpan.FromMinutes(15))
        {
            if (Scheduler is IAmAMessageSchedulerAsync async)
            {
                await async.ScheduleAsync(message, delay.Value, cancellationToken);
                return;
            }

            if (Scheduler is IAmAMessageSchedulerSync sync)
            {
                sync.Schedule(message, delay.Value);
                return;
            }
                
            s_logger.LogWarning("SQSMessageProducer: no scheduler configured, message will be sent immediately");
        }
        
        s_logger.LogDebug(
            "SQSMessageProducer: Publishing message with topic {Topic} and id {Id} and message: {Request}",
            message.Header.Topic, message.Id, message.Body);

        await ConfirmQueueExistsAsync(message.Header.Topic, cancellationToken);

        using var client = _clientFactory.CreateSqsClient();
        var type = _publication.SqsAttributes?.Type ?? SnsSqsType.Standard;
        var sender = new SqsMessageSender(ChannelQueueUrl!, type, client);
        var messageId = await sender.SendAsync(message, delay, cancellationToken);

        if (messageId == null)
        {
            throw new InvalidOperationException(
                $"Failed to publish message with topic {message.Header.Topic} and id {message.Id} and message: {message.Body}");
        }

        s_logger.LogDebug(
            "SQSMessageProducer: Published message with topic {Topic}, Brighter messageId {MessageId} and SNS messageId {SNSMessageId}",
            message.Header.Topic, message.Id, messageId);
    }

    public void Send(Message message) => SendWithDelay(message, null);

    public void SendWithDelay(Message message, TimeSpan? delay)
        => BrighterAsyncContext.Run(async () => await SendWithDelayAsync(message, delay));
}
