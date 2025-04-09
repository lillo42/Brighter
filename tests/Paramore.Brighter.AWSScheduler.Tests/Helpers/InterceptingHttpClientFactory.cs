﻿using Amazon.Runtime;

namespace Paramore.Brighter.AWSScheduler.Tests.Helpers;

internal sealed class InterceptingHttpClientFactory(InterceptingDelegatingHandler handler) : HttpClientFactory
{
    public override HttpClient CreateHttpClient(IClientConfig clientConfig)
    {
        handler.InnerHandler ??= new HttpClientHandler();
        return new HttpClient(handler);
    }
}
