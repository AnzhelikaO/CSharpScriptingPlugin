namespace CSharpScripting.Configuration.Prefixes;

[UsedImplicitly]
public sealed class ExecutePrefix : CodePrefix
{
    private const string USING = "using";
    private static readonly Regex USING_REGEX = new($@"^(?:\s*(?<{USING}>using\s+[^;]+;?))+\s*$");
    protected override string PrefixInner => DEFAULT_PREFIX;

    protected override async Task HandleInner(TSPlayer Sender, CSEnvironment Environment,
                                              string Using, string Code, CodeManager CodeManager,
                                              ScriptOptions Options, Globals Globals)
    {
        Match @using = USING_REGEX.Match(Code);
        if (@using.Success)
        {
            await CSharpScript.RunAsync(Code, Options, Globals); // run to validate
            await Environment.AddUsing(@using.Groups[USING].Captures.Select(c => c.Value));
            return;
        }

        if ((await CSharpScript.RunAsync($"{Using}\n{Code}", Options, Globals))
                .ReturnValue is object value)
            Globals.cw(value);
    }
}