namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// Contains constant string values representing well-known and custom message header keys
/// used within the Brighter ActiveMQ integration.
/// </summary>
/// <remarks>
/// Most of these headers correspond to the CloudEvents specification, while others are
/// specific to Brighter or the ActiveMQ (NMS) client, such as the ReceiptHandle.
/// </remarks>
public static class HeaderNames
{
    /// <summary>
    /// Gets the key for the message's content type (often JSON, XML, etc.). Corresponds to the CloudEvents <c>datacontenttype</c>.
    /// </summary>
    public const string DataContentType = "datacontenttype";
    
    /// <summary>
    /// Gets the key for a URI pointing to a schema that the message data adheres to. Corresponds to the CloudEvents <c>dataschema</c>.
    /// </summary>
    public const string DataSchema = "dataschema";
    
    /// <summary>
    /// Gets the key for the unique identifier of the message. Corresponds to the CloudEvents <c>id</c>.
    /// </summary>
    public const string Id = "id";
    
    /// <summary>
    /// Gets the key for the type of the message (e.g., <c>Command</c>, <c>Event</c>, or <c>Request</c>).
    /// </summary>
    public const string MessageType = "messagetype";
    
    /// <summary>
    /// Gets the key for the CloudEvents specification version used by the message. Corresponds to the CloudEvents <c>specversion</c>.
    /// </summary>
    public const string SpecVersion = "specversion";
    
    /// <summary>
    /// Gets the key for the URI of the context in which an event happened. Corresponds to the CloudEvents <c>source</c>.
    /// </summary>
    public const string Source = "source";
    
    /// <summary>
    /// Gets the key for a short, human-readable summary of the event. Corresponds to the CloudEvents <c>subject</c>.
    /// </summary>
    public const string Subject = "subject";
    
    /// <summary>
    /// Gets the key for the routing key or topic the message was sent on.
    /// </summary>
    public const string Topic = "topic";
    
    /// <summary>
    /// Gets the key for the timestamp when the event or message was produced. Corresponds to the CloudEvents <c>timestamp</c>.
    /// </summary>
    public const string TimeStamp = "timestamp";
    
    /// <summary>
    /// Gets the key for the handle used by the broker to identify the message for acknowledgement or rejection.
    /// This is an ActiveMQ-specific header.
    /// </summary>
    public const string ReceiptHandle = "ReceiptHandle";
    
    /// <summary>
    /// Gets the key for a custom Brighter header specifying whether a <c>ReplyTo</c> address
    /// should be treated as a Queue or a Topic.
    /// </summary>
    public const string ReplyToType = "Brighter-ActiveMQ-ReplyToType";
}
