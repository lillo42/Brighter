namespace Paramore.Brighter.ActiveMq.Tests.TestDoubles;

internal class MyDeferredCommand() : Command(Id.Random())
{
    public string? Value { get; set; }
}
