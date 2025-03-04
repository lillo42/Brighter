﻿#region Licence
/* The MIT License (MIT)
Copyright © 2024 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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

using System.Collections.Generic;

namespace Paramore.Brighter.MessagingGateway.Kafka;

public static class BrighterDefinedHeaders
{
    public static readonly HashSet<string> HeadersToReset;

    static BrighterDefinedHeaders()
    {
        HeadersToReset =
        [
            HeaderNames.MESSAGE_ID,
            HeaderNames.MESSAGE_TYPE,
            HeaderNames.TOPIC,
            HeaderNames.CORRELATION_ID,
            HeaderNames.TIMESTAMP,
            HeaderNames.PARTITIONKEY,
            HeaderNames.CONTENT_TYPE,
            HeaderNames.REPLY_TO,
            HeaderNames.DELAYED_MILLISECONDS,
            HeaderNames.HANDLED_COUNT,
            Paramore.Brighter.BrighterHeaderNames.UseCloudEvents
        ];  
    }

}
