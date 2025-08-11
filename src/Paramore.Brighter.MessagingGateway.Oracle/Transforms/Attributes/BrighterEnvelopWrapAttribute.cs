using System;
using Paramore.Brighter.MessagingGateway.Oracle.Transforms.Transformers;

namespace Paramore.Brighter.MessagingGateway.Oracle.Transforms.Attributes;

public class BrighterEnvelopWrapAttribute(int step, BrighterEnvelopType type) : WrapWithAttribute(step)
{
    public BrighterEnvelopType Type { get; } = type;
    
    /// <inheritdoc />
    public override object?[] InitializerParams() => [Type];

    /// <inheritdoc />
    public override Type GetHandlerType()
    {
        return typeof(BrighterEnvelopTransformer);
    }
}


public enum BrighterEnvelopType
{
    Json,
    Xml
}
