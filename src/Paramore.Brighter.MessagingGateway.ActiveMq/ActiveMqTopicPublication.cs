namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// Represents a Brighter <see cref="Publication"/> configuration for sending a message
/// to a specific **ActiveMQ Topic**.
/// </summary>
/// <remarks>
/// Messages published with this configuration will be routed to a Topic destination,
/// typically implementing publish/subscribe messaging semantics.
/// </remarks>
public class ActiveMqTopicPublication : ActiveMqPublication;

/// <summary>
/// Represents a strongly-typed Brighter <see cref="Publication"/> configuration for sending a specific
/// message type to an ActiveMQ Topic.
/// </summary>
/// <typeparam name="T">The type of the request (<see cref="IRequest"/>) that will be published to the Topic.</typeparam>
public class ActiveMqTopicPublication<T> : ActiveMqTopicPublication 
    where T : class, IRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveMqTopicPublication{T}"/> class
    /// and automatically sets the <see cref="P:Paramore.Brighter.Publication.RequestType"/> based on the generic type parameter <typeparamref name="T"/>.
    /// </summary>
    public ActiveMqTopicPublication()
    {
        RequestType = typeof(T);
    }
}
