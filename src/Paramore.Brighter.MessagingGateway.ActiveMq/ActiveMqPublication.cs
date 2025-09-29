using System;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// An abstract base class for Brighter <see cref="Publication"/> configurations
/// that target an ActiveMQ broker (or any broker using the NMS API).
/// </summary>
/// <remarks>
/// This class extends the standard <see cref="Publication"/> properties with ActiveMQ-specific
/// settings like delivery mode, priority, and time-to-live.
/// </remarks>
public abstract class ActiveMqPublication : Publication
{
    /// <summary>
    /// Gets or sets the time provider used for time-related calculations, such as message expiry.
    /// </summary>
    /// <value>
    /// An instance of <see cref="TimeProvider"/>. If <see langword="null"/>, the system default will be used.
    /// </value>
    public TimeProvider? TimeProvider { get; set; }

    /// <summary>
    /// Gets or sets the message delivery mode for the published message.
    /// </summary>
    /// <value>
    /// A <see cref="MsgDeliveryMode"/> value, typically Persistent or NonPersistent.
    /// </value>
    public MsgDeliveryMode DeliveryMode { get; set; }

    /// <summary>
    /// Gets or sets the priority of the message when it is sent to the ActiveMQ broker.
    /// </summary>
    /// <value>
    /// A <see cref="MsgPriority"/> value (0-9, with 4 being the default).
    /// </value>
    public MsgPriority Priority { get; set; }

    /// <summary>
    /// Gets or sets the duration after which the published message should be considered expired by the broker.
    /// </summary>
    /// <value>
    /// A <see cref="TimeSpan"/> representing the message's time-to-live.
    /// </value>
    public TimeSpan TimeToLive { get; set; }
}
