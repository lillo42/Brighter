
namespace Paramore.Brighter;

public interface IAmARoutingKeyResolver
{
    RoutingKey? Resolve<TRequest>()
        where TRequest : class, IRequest;
}
