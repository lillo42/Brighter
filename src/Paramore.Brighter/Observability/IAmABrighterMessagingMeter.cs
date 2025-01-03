#region Licence

/* The MIT License (MIT)
Copyright © 2025 Tim Salva <tim@jtsalva.dev>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System.Diagnostics;

namespace Paramore.Brighter.Observability;

public interface IAmABrighterMessagingMeter
{
    /// <summary>
    /// Record duration of messaging operation initiated by a producer or consumer client.
    /// SHOULD NOT be used to report processing duration - processing duration is reported in RecordProcess
    /// </summary>
    /// <param name="activity">Activity to record</param>
    void RecordClientOperation(Activity activity);

    /// <summary>
    /// Add number of messages producer attempted to send to the broker.
    /// </summary>
    /// <param name="activity">Activity to add</param>
    void AddClientSentMessage(Activity activity);

    /// <summary>
    /// Add number of messages that were delivered to the application.
    /// </summary>
    /// <param name="activity">Activity to add</param>
    void AddClientConsumedMessage(Activity activity);

    /// <summary>
    /// Record duration of processing operation.
    /// </summary>
    /// <param name="activity">Activity to record</param>
    void RecordProcess(Activity activity);
    
    /// <summary>
    /// Checks if any of the instrument members has any listeners
    /// For example, this will be false if the associated meters are not registered
    /// </summary>
    bool Enabled { get; }
}