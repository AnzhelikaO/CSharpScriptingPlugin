namespace CSharpScripting.Configuration.Prefixes;

[UsedImplicitly]
public sealed class SignatureTypePrefix : SignaturePrefix
{
    protected override string PrefixInner => $"{DEFAULT_PREFIX}{ADDITIONAL_PREFIX}{ADDITIONAL_PREFIX}";
    protected internal override bool AddSemicolon => false;

    protected override async Task HandleInner(TSPlayer Sender, CSEnvironment Environment,
                                              string Using, string Code, CodeManager CodeManager,
                                              ScriptOptions Options, Globals Globals) =>
        Show(Globals, (Type)(await CSharpScript.RunAsync($"{Using}\nreturn typeof({Code});",
                                                         Options, Globals))
                            .ReturnValue);
}