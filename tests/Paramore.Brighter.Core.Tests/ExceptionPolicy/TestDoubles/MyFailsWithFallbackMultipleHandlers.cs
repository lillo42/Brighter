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
using Paramore.Brighter.Core.Tests.CommandProcessors.TestDoubles;
using Paramore.Brighter.Logging.Attributes;
using Paramore.Brighter.Policies.Attributes;
using Paramore.Brighter.Policies.Handlers;

namespace Paramore.Brighter.Core.Tests.ExceptionPolicy.TestDoubles
{
    internal sealed class MyFailsWithFallbackMultipleHandlers : RequestHandler<MyCommand>
    {
        public static bool FallbackCalled { get; set; }
        public static bool ReceivedCommand { get; set; }
        public static bool SetException { get; set; }

        static MyFailsWithFallbackMultipleHandlers ()
        {
            ReceivedCommand = false;
        }

        [FallbackPolicy(true, false, 1)]
        [RequestLogging(2, HandlerTiming.Before)]
        public override MyCommand Handle(MyCommand command)
        {
            ReceivedCommand = true;
            throw new DivideByZeroException();
        }

        public override MyCommand Fallback(MyCommand command)
        {
            FallbackCalled = true;
            if (Context.Bag.ContainsKey(FallbackPolicyHandler<MyCommand>.CAUSE_OF_FALLBACK_EXCEPTION))
                SetException = true;
            return base.Fallback(command);
        }

        public static bool ShouldFallback(MyCommand command)
        {
            return FallbackCalled;
        }

        public static bool ShouldReceive(MyCommand myCommand)
        {
            return ReceivedCommand;
        }

        public static bool ShouldSetException(MyCommand myCommand)
        {
            return SetException;
        }
    }
}
