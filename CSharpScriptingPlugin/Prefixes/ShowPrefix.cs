namespace CSharpScripting.Configuration.Prefixes;

[UsedImplicitly]
public sealed class ShowPrefix : CodePrefix
{
    protected override string PrefixInner => ";;";
    protected override async Task HandleInner(TSPlayer Sender, string Code,
                                              ScriptOptions Options, Globals Globals) =>
        await CSharpScript.RunAsync($"return {Code}", Options, Globals)
                          .ContinueWith(s => Globals.cw(s.Result.ReturnValue));
}