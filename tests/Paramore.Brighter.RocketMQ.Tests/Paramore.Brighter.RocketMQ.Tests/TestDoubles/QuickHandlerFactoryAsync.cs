﻿namespace Paramore.Brighter.RocketMQ.Tests.TestDoubles;

internal class QuickHandlerFactoryAsync(Func<IHandleRequestsAsync> handlerAction) : IAmAHandlerFactoryAsync
{
    public IHandleRequestsAsync Create(Type handlerType, IAmALifetime lifetime)
    {
        return handlerAction();
    }

    public void Release(IHandleRequestsAsync handler, IAmALifetime lifetime) { }
}
