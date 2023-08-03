namespace CSharpScriptingPlugin.Configuration;

public abstract class As
{
    public abstract dynamic? Transform(Dictionary<dynamic, dynamic> Dictionary);
}
public sealed class AsValuesArray : As
{
    internal AsValuesArray() { }
    public override dynamic Transform(Dictionary<dynamic, dynamic> Dictionary) =>
        Dictionary.Values.ToArray();
}
public sealed class AsValuesList : As
{
    internal AsValuesList() { }
    public override dynamic Transform(Dictionary<dynamic, dynamic> Dictionary) =>
        Dictionary.Values.ToList();
}
public sealed class AsValuesTuple : As
{
    internal AsValuesTuple() { }
    public override dynamic Transform(Dictionary<dynamic, dynamic> Dictionary) =>
        CreateValueTuple(Dictionary.Values);
}