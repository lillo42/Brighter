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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Brighter
{
    /// <summary>
    /// Interface IAmACommandProcessor
    /// Paramore.Brighter provides the default implementation of this interface <see cref="CommandProcessor"/> and it is unlikely you need
    /// to override this for anything other than testing purposes. The usual need is that in a <see cref="RequestHandler{T}"/> you intend to publish an  
    /// <see cref="Event"/> to indicate the handler has completed to other components. In this case your tests should only verify that the correct 
    /// event was raised by listening to <see cref="Publish{T}"/> calls on this interface, using a mocking framework of your choice or bespoke
    /// Test Double.
    /// </summary>
    public interface IAmACommandProcessor
    {
        /// <summary>
        /// Sends the specified command.
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <param name="command">The command.</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        void Send<TRequest>(TRequest command, RequestContext? requestContext = null) where TRequest : class, IRequest;

        /// <summary>
        /// Awaitably sends the specified command.
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <param name="command">The command.</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>        /// <param name="continueOnCapturedContext">Should we use the calling thread's synchronization context when continuing or a default thread synchronization context. Defaults to false</param>
        /// <param name="cancellationToken">Allows the sender to cancel the request pipeline. Optional</param>
        /// <returns>awaitable <see cref="Task"/>.</returns>
        Task SendAsync<TRequest>(TRequest command, RequestContext? requestContext = null, bool continueOnCapturedContext = true, CancellationToken cancellationToken = default) where TRequest : class, IRequest;

        /// <summary>
        /// Publishes the specified event. Throws an aggregate exception on failure of a pipeline but executes remaining
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>       /// <param name="event">The event.</param>
        void Publish<TRequest>(TRequest @event, RequestContext? requestContext = null) where TRequest : class, IRequest;

        /// <summary>
        /// Publishes the specified event with async/await support. Throws an aggregate exception on failure of a pipeline but executes remaining
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <param name="event">The event.</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="continueOnCapturedContext">Should we use the calling thread's synchronization context when continuing or a default thread synchronization context. Defaults to false</param>
        /// <param name="cancellationToken">Allows the sender to cancel the request pipeline. Optional</param>
        /// <returns>awaitable <see cref="Task"/>.</returns>
        Task PublishAsync<TRequest>(
            TRequest @event, 
            RequestContext? requestContext = null,
            bool continueOnCapturedContext = true, 
            CancellationToken cancellationToken = default
            ) where TRequest : class, IRequest;

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="args">For transports or outboxes that require additional parameters such as topic, provide an optional arg</param>
        void Post<TRequest>(TRequest request, RequestContext? requestContext= null, Dictionary<string, object>? args = null) where TRequest : class, IRequest;

        /// <summary>
        /// Posts the specified request with async/await support.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="continueOnCapturedContext">Should we use the calling thread's synchronization context when continuing or a default thread synchronization context. Defaults to false</param>
        /// <param name="args">For transports or outboxes that require additional parameters such as topic, provide an optional arg</param>
        /// <param name="cancellationToken">Allows the sender to cancel the request pipeline. Optional</param>
        /// <returns>awaitable <see cref="Task"/>.</returns>
        Task PostAsync<TRequest>(
            TRequest request, 
            RequestContext? requestContext = null,
            Dictionary<string, object>? args = null,
            bool continueOnCapturedContext = true, 
            CancellationToken cancellationToken = default
        ) where TRequest : class, IRequest;

        /// <summary>
        /// Adds a message into the outbox, and returns the id of the saved message.
        /// Intended for use with the Outbox pattern: http://gistlabs.com/2014/05/the-outbox/ normally you include the
        /// call to DepositPostBox within the scope of the transaction to write corresponding entity state to your
        /// database, that you want to signal via the request to downstream consumers
        /// Pass deposited message to <see cref="CommandProcessor.ClearOutbox"/> 
        /// </summary>
        /// <param name="request">The request to save to the outbox</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="args">For transports or outboxes that require additional parameters such as topic, provide an optional arg</param>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <returns></returns>
        string DepositPost<TRequest>(TRequest request, RequestContext? requestContext = null, Dictionary<string, object>? args = null) where TRequest : class, IRequest;

        /// <summary>
        /// Adds a message into the outbox, and returns the id of the saved message.
        /// Intended for use with the Outbox pattern: http://gistlabs.com/2014/05/the-outbox/ normally you include the
        /// call to DepositPostBox within the scope of the transaction to write corresponding entity state to your
        /// database, that you want to signal via the request to downstream consumers
        /// Pass deposited message to <see cref="CommandProcessor.ClearOutbox"/> 
        /// </summary>
        /// <param name="request">The request to save to the outbox</param>
        /// <param name="transactionProvider">If using an Outbox, the transaction provider for the Outbox</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="args">For transports or outboxes that require additional parameters such as topic, provide an optional arg</param>
        /// <param name="batchId">The id of the deposit batch, if this isn't set items will be added to the outbox as they come in and not as a batch</param>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <typeparam name="TTransaction">The type of transaction used by the outbox</typeparam>
        /// <returns></returns>
        string DepositPost<TRequest, TTransaction>(
            TRequest request,
            IAmABoxTransactionProvider<TTransaction> transactionProvider,
            RequestContext? requestContext = null,
            Dictionary<string, object>? args = null,
            string? batchId = null
            ) where TRequest : class, IRequest;

        /// <summary>
        /// Adds a messages into the outbox, and returns the id of the saved message.
        /// Intended for use with the Outbox pattern: http://gistlabs.com/2014/05/the-outbox/ normally you include the
        /// call to DepositPostBox within the scope of the transaction to write corresponding entity state to your
        /// database, that you want to signal via the request to downstream consumers
        /// Pass deposited message to <see cref="ClearOutbox"/> 
        /// </summary>
        /// <param name="requests">The requests to save to the outbox</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="args">For transports or outboxes that require additional parameters such as topic, provide an optional arg</param>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <returns>The Id of the Message that has been deposited.</returns>
        string[] DepositPost<TRequest>(IEnumerable<TRequest> requests, RequestContext requestContext, Dictionary<string, object>? args = null) where TRequest : class, IRequest;

        /// <summary>
        /// Adds a messages into the outbox, and returns the id of the saved message.
        /// Intended for use with the Outbox pattern: http://gistlabs.com/2014/05/the-outbox/ normally you include the
        /// call to DepositPostBox within the scope of the transaction to write corresponding entity state to your
        /// database, that you want to signal via the request to downstream consumers
        /// Pass deposited message to <see cref="ClearOutbox(System.string[])"/> 
        /// </summary>
        /// <param name="requests">The requests to save to the outbox</param>
        /// <param name="transactionProvider">If using an Outbox, the transaction provider for the Outbox</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="args">For transports or outboxes that require additional parameters such as topic, provide an optional arg</param>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <typeparam name="TTransaction">The type of transaction used by the outbox</typeparam>
        /// <returns>The Id of the Message that has been deposited.</returns>
        string[] DepositPost<TRequest, TTransaction>(
            IEnumerable<TRequest> requests,
            IAmABoxTransactionProvider<TTransaction> transactionProvider,
            RequestContext? requestContext = null,
            Dictionary<string, object>? args = null
            ) where TRequest : class, IRequest;

        /// <summary>
        /// Adds a message into the outbox, and returns the id of the saved message.
        /// Intended for use with the Outbox pattern: http://gistlabs.com/2014/05/the-outbox/ normally you include the
        /// call to DepositPostBox within the scope of the transaction to write corresponding entity state to your
        /// database, that you want to signal via the request to downstream consumers
        /// Pass deposited message to <see cref="CommandProcessor.ClearOutboxAsync"/> 
        /// </summary>
        /// <param name="request">The request to save to the outbox</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="args">For outboxes that require additional parameters such as topic, provide an optional arg</param>
        /// <param name="continueOnCapturedContext">Should we use the calling thread's synchronization context when continuing or a default thread synchronization context. Defaults to false</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <returns></returns>
        Task<string> DepositPostAsync<TRequest>(
            TRequest request,
            RequestContext? requestContext = null,
            Dictionary<string, object>? args = null,
            bool continueOnCapturedContext = true,
            CancellationToken cancellationToken = default
            ) where TRequest : class, IRequest;


        /// <summary>
        /// Adds a message into the outbox, and returns the id of the saved message.
        /// Intended for use with the Outbox pattern: http://gistlabs.com/2014/05/the-outbox/ normally you include the
        /// call to DepositPostBox within the scope of the transaction to write corresponding entity state to your
        /// database, that you want to signal via the request to downstream consumers
        /// Pass deposited message to <see cref="CommandProcessor.ClearOutboxAsync"/> 
        /// </summary>
        /// <param name="request">The request to save to the outbox</param>
        /// <param name="transactionProvider">If using an Outbox, the transaction provider for the Outbox</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="args">For transports or outboxes that require additional parameters such as topic, provide an optional arg</param>
        /// <param name="continueOnCapturedContext">Should we use the calling thread's synchronization context when continuing or a default thread synchronization context. Defaults to false</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        /// <param name="batchId">The id of the deposit batch, if this isn't set items will be added to the outbox as they come in and not as a batch</param>
        /// <typeparam name="T">The type of the request</typeparam>
        /// <typeparam name="TTransaction">The type of transaction used by the outbox</typeparam>
        /// <returns></returns>
        Task<string> DepositPostAsync<T, TTransaction>(
            T request,
            IAmABoxTransactionProvider<TTransaction> transactionProvider,
            RequestContext? requestContext = null,
            Dictionary<string, object>? args = null,
            bool continueOnCapturedContext = true,
            CancellationToken cancellationToken = default,
            string? batchId = null
            ) where T : class, IRequest;

        /// <summary>
        /// Adds a message into the outbox, and returns the id of the saved message.
        /// Intended for use with the Outbox pattern: http://gistlabs.com/2014/05/the-outbox/ normally you include the
        /// call to DepositPostBox within the scope of the transaction to write corresponding entity state to your
        /// database, that you want to signal via the request to downstream consumers
        /// Pass deposited message to <see cref="ClearOutboxAsync"/> 
        /// </summary>
        /// <param name="requests">The requests to save to the outbox</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="args">For transports or outboxes that require additional parameters such as topic, provide an optional arg</param>
        /// <param name="continueOnCapturedContext">Should we use the calling thread's synchronization context when continuing or a default thread synchronization context. Defaults to false</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <typeparam name="TTransaction">The type of transaction used by the outbox</typeparam>
        /// <returns></returns>
        Task<string[]> DepositPostAsync<TRequest>(
            IEnumerable<TRequest> requests,
            RequestContext? requestContext = null,
            Dictionary<string, object>? args = null,
            bool continueOnCapturedContext = true,
            CancellationToken cancellationToken = default
            ) where TRequest : class, IRequest;

        /// <summary>
        /// Adds a message into the outbox, and returns the id of the saved message.
        /// Intended for use with the Outbox pattern: http://gistlabs.com/2014/05/the-outbox/ normally you include the
        /// call to DepositPostBox within the scope of the transaction to write corresponding entity state to your
        /// database, that you want to signal via the request to downstream consumers
        /// Pass deposited message to <see cref="ClearOutboxAsync"/> 
        /// </summary>
        /// <param name="requests">The requests to save to the outbox</param>
        /// <param name="transactionProvider">If using an Outbox, the transaction provider for the Outbox</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="args">For transports or outboxes that require additional parameters such as topic, provide an optional arg</param>
        /// <param name="continueOnCapturedContext">Should we use the calling thread's synchronization context when continuing or a default thread synchronization context. Defaults to false</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        /// <typeparam name="T">The type of the request</typeparam>
        /// <typeparam name="TTransaction">The type of transaction used by the outbox</typeparam>
        /// <returns></returns>
        Task<string[]> DepositPostAsync<T, TTransaction>(
            IEnumerable<T> requests,
            IAmABoxTransactionProvider<TTransaction> transactionProvider,
            RequestContext? requestContext = null,
            Dictionary<string, object>? args = null,
            bool continueOnCapturedContext = true,
            CancellationToken cancellationToken = default
            ) where T : class, IRequest;

        /// <summary>
        /// Flushes the message box message given by <param name="ids"/> to the broker.
        /// Intended for use with the Outbox pattern: http://gistlabs.com/2014/05/the-outbox/ <see cref="DepositPostBox"/>
        /// </summary>
        /// <param name="ids">The ids to flush</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="args">For transports or outboxes that require additional parameters such as topic, provide an optional arg</param>
        void ClearOutbox(string[] ids, RequestContext? requestContext = null, Dictionary<string, object>? args = null);

        /// <summary>
        /// Flushes the message box message given by <param name="posts"/> to the broker.
        /// Intended for use with the Outbox pattern: http://gistlabs.com/2014/05/the-outbox/ <see cref="DepositPostBoxAsync"/>
        /// </summary>
        /// <param name="posts">The ids to flush</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/> </param>
        /// <param name="args">For transports or outboxes that require additional parameters such as topic, provide an optional arg</param>
        /// <param name="continueOnCapturedContext"></param>
        /// <param name="cancellationToken"></param>
        Task ClearOutboxAsync(
            IEnumerable<string> posts,
            RequestContext? requestContext = null,
            Dictionary<string, object>? args = null,
            bool continueOnCapturedContext = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uses the Request-Reply messaging approach to send a message to another server and block awaiting a reply.
        /// The message is placed into a message queue but not into the outbox.
        /// An ephemeral reply queue is created, and its name used to set the reply address for the response. We produce
        /// a queue per exchange, to simplify correlating send and receive.
        /// The response is directed to a registered handler.
        /// Because the operation blocks, there is a mandatory timeout
        /// </summary>
        /// <param name="request">What message do we want a reply to</param>
        /// <param name="requestContext">The context of the request; if null we will start one via a <see cref="RequestContextFactory"/></param>
        /// <param name="timeOut">The call blocks, so we must time out; defaults to 500 ms if null</param>
        /// <exception cref="NotImplementedException"></exception>
        TResponse? Call<T, TResponse>(T request, RequestContext? requestContext = null, TimeSpan? timeOut = null)
            where T : class, ICall where TResponse : class, IResponse;
    }
}
