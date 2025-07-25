#region Licence
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

namespace Paramore.Brighter
{
    /// <summary>
    /// Interface IAmAPipelineBuilder
    /// Builds the pipeline that handles an <see cref="IRequest"/>, with a target <see cref="IHandleRequests"/> and any orthogonal <see cref="IHandleRequests"/> for
    /// Quality of Service (qos)
    /// The default implementation is <see cref="PipelineBuilder{T}"/>
    /// </summary>
    /// <typeparam name="TRequest">The type of the t request.</typeparam>
    internal interface IAmAPipelineBuilder<TRequest> : IDisposable where TRequest : class, IRequest
    {
        /// <summary>
        /// Builds the specified request context.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/> that we are building the pipeline for</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>Pipelines&lt;TRequest&gt;.</returns>
        Pipelines<TRequest> Build(TRequest request, IRequestContext requestContext);
    }
}
