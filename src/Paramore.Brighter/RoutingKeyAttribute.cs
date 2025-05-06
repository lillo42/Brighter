
using System;

namespace Paramore.Brighter;

/// <summary>
/// An attribute that allows you to specify the routing key for a command or event type within the Brighter framework.
/// This attribute is applied to a class and provides the routing key used when publishing messages of that type.
/// </summary>
/// <remarks>
/// <para>
/// In Brighter, the routing key is a crucial element for directing messages to the appropriate consumers.
/// This attribute simplifies the configuration of routing keys by allowing you to define them directly on the
/// command or event class itself.
/// </para>
/// <para>
/// By applying the <see cref="RoutingKeyAttribute"/> to a class, you eliminate the need for separate configuration
/// of routing keys, making your code more self-descriptive and easier to manage.
/// </para>
/// </remarks>
/// <example>
/// [RoutingKey("user.created")]
/// public class UserCreatedEvent : Event
/// {
///     public Guid UserId { get; set; }
///     public string UserName { get; set; }
/// }
///
/// [RoutingKey("assigne.task")]
/// public class AssignTaskCommand : Command
/// {
///     public Guid TaskId { get; set; }
///     public Guid AssigneeId { get; set; }
/// }
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class RoutingKeyAttribute(string routingKey) : Attribute
{
    /// <summary>
    /// Gets the routing key specified by the attribute.
    /// </summary>
    /// <value>
    /// The routing key as a <see cref="string"/>.
    /// </value>
    public string RoutingKey { get; } = routingKey;
}
