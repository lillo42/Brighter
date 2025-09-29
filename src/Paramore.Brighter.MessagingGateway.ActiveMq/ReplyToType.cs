namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// Specifies the type of destination to use for the <c>ReplyTo</c> address in a message header.
/// </summary>
/// <remarks>
/// This determines whether the response message should be routed to a Queue (point-to-point) or a Topic (publish/subscribe).
/// </remarks>
public enum ReplyToType
{
    /// <summary>
    /// The reply destination is an ActiveMQ **Queue**, typically used for a single recipient 
    /// in a request-response pattern.
    /// </summary>
    Queue,
    
    /// <summary>
    /// The reply destination is an ActiveMQ **Topic**, typically used if the reply 
    /// needs to be broadcast to multiple subscribers.
    /// </summary>
    Topic
}
