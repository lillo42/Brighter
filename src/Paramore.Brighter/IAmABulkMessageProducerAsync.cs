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
    /// Interface IAmABulkMessageProducerAsync
    /// Abstracts away the Application Layer used to push messages with async/await support onto a <a href="http://parlab.eecs.berkeley.edu/wiki/_media/patterns/taskqueue.pdf">Task Queue</a>
    /// Usually clients do not need to instantiate as access is via an <see cref="IAmAChannelSync"/> derived class.
    /// We provide the following default gateway applications
    /// <list type="bullet">
    /// <item>AMQP</item>
    /// <item>RESTML</item>
    /// </list>
    /// </summary>
    public interface IAmABulkMessageProducerAsync : IAmAMessageProducer
    {
        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        IAsyncEnumerable<Id[]> SendAsync(IEnumerable<Message> messages, CancellationToken cancellationToken);
    }
}
