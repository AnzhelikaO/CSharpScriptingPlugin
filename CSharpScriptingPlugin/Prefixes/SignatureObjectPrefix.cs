namespace CSharpScripting.Configuration.Prefixes;

[UsedImplicitly]
public sealed class SignatureObjectPrefix : SignaturePrefix
{
    protected override string PrefixInner => $"{DEFAULT_PREFIX}{ADDITIONAL_PREFIX}";

    protected override async Task HandleInner(TSPlayer Sender, CSEnvironment Environment,
                                              string Using, string Code, CodeManager CodeManager,
                                              ScriptOptions Options, Globals Globals) =>
        Show(Globals, (await CSharpScript.RunAsync($"{Using}\nreturn {Code}", Options, Globals))
                      .ReturnValue
                      .GetType());
}