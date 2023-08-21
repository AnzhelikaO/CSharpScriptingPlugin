namespace CSharpScripting.Configuration.Prefixes;

[UsedImplicitly]
public sealed class SignatureTypePrefix : SignaturePrefix
{
    protected override string PrefixInner => $"{DEFAULT_PREFIX}{ADDITIONAL_PREFIX}{ADDITIONAL_PREFIX}";
    protected internal override bool AddSemicolon => false;

    protected override async Task HandleInner(TSPlayer Sender, string Code, CodeManager CodeManager,
                                              ScriptOptions Options, Globals Globals) =>
        await CSharpScript.RunAsync($"return typeof({Code});", Options, Globals)
                          .ContinueWith(s => ((Type)s.Result.ReturnValue)
                                             .GetMembers()
                                             .ForEach(m => Globals.cw(m)));
}