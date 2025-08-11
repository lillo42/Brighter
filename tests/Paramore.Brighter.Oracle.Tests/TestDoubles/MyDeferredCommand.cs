namespace Paramore.Brighter.Oracle.Tests.TestDoubles;

internal class MyDeferredCommand() : Command(Id.Random())
{
    public string? Value { get; set; }
}
