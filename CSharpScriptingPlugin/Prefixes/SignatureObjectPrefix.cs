namespace CSharpScripting.Configuration.Prefixes;

[UsedImplicitly]
public sealed class SignatureObjectPrefix : SignaturePrefix
{
    protected override string PrefixInner => $"{DEFAULT_PREFIX}{ADDITIONAL_PREFIX}";

    protected override async Task HandleInner(TSPlayer Sender, string Code, CodeManager CodeManager,
                                              ScriptOptions Options, Globals Globals) =>
        await CSharpScript.RunAsync($"return {Code}", Options, Globals)
                          .ContinueWith(s => s.Result
                                              .ReturnValue
                                              .GetType()
                                              .GetMembers()
                                              .ForEach(m => Globals.cw(m)));
}