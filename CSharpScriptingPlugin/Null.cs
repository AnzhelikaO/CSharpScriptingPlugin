namespace CSharpScripting.Configuration.SpecialNull;

public sealed record Null
{
    public static readonly Null Instance = new();
    private Null() { }
}