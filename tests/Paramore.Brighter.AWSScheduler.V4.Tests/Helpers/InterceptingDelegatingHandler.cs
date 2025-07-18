﻿using System.Collections.Concurrent;

namespace Paramore.Brighter.AWSScheduler.V4.Tests.Helpers;

internal sealed class InterceptingDelegatingHandler(string tag) : DelegatingHandler
{
    public static ConcurrentDictionary<string, int> RequestCount { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!RequestCount.TryAdd(tag, 1))
        {
            RequestCount[tag] += 1;
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
