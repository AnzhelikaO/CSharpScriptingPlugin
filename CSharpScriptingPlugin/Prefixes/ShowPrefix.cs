namespace CSharpScripting.Configuration.Prefixes;

[UsedImplicitly]
public sealed class ShowPrefix : CodePrefix
{
    public new const string DEFAULT_PREFIX = $"{CodePrefix.DEFAULT_PREFIX}{CodePrefix.DEFAULT_PREFIX}";
    protected override string PrefixInner => DEFAULT_PREFIX;

    protected override async Task HandleInner(TSPlayer Sender, CSEnvironment Environment,
                                              string Using, string Code, CodeManager CodeManager,
                                              ScriptOptions Options, Globals Globals) =>
        Globals.cw((await CSharpScript.RunAsync($"{Using}\nreturn {Code}", Options, Globals)).ReturnValue);
}