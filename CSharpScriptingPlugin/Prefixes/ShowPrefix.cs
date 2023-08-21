namespace CSharpScripting.Configuration.Prefixes;

[UsedImplicitly]
public sealed class ShowPrefix : CodePrefix
{
    public new const string DEFAULT_PREFIX = $"{CodePrefix.DEFAULT_PREFIX}{CodePrefix.DEFAULT_PREFIX}";
    protected override string PrefixInner => DEFAULT_PREFIX;

    protected override async Task HandleInner(TSPlayer Sender, string Code, CodeManager CodeManager,
                                              ScriptOptions Options, Globals Globals) =>
        await CSharpScript.RunAsync($"return {Code}", Options, Globals)
                          .ContinueWith(s => Globals.cw(s.Result.ReturnValue));
}