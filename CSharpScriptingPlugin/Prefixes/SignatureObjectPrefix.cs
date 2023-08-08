namespace CSharpScripting.Configuration.Prefixes;

[UsedImplicitly]
public sealed class SignatureObjectPrefix : CodePrefix
{
    protected override string PrefixInner => ";=";
    protected override async Task HandleInner(TSPlayer Sender, string Code,
                                              ScriptOptions Options, Globals Globals) =>
        await CSharpScript.RunAsync($"return {Code}", Options, Globals)
                          .ContinueWith(s => s.Result
                                              .ReturnValue
                                              .GetType()
                                              .GetMembers()
                                              .ForEach(m => Globals.cw(m)));
}