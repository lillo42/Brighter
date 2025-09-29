namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// Represents a Brighter <see cref="Publication"/> configuration for sending a message
/// to a specific **ActiveMQ Queue**.
/// </summary>
/// <remarks>
/// Messages published with this configuration will be routed to a Queue destination,
/// typically implementing point-to-point messaging semantics.
/// </remarks>
public class ActiveMqQueuePublication : ActiveMqPublication;

/// <summary>
/// Represents a strongly-typed Brighter <see cref="Publication"/> configuration for sending a specific
/// message type to an ActiveMQ Queue.
/// </summary>
/// <typeparam name="T">The type of the request (<see cref="IRequest"/>) that will be sent to the Queue.</typeparam>
public class ActiveMqQueuePublication<T> : ActiveMqQueuePublication 
    where T : class, IRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveMqQueuePublication{T}"/> class
    /// and automatically sets the <see cref="P:Paramore.Brighter.Publication.RequestType"/> based on the generic type parameter <typeparamref name="T"/>.
    /// </summary>
    public ActiveMqQueuePublication()
    {
        RequestType = typeof(T);
    }
}
