namespace CSharpScripting.Configuration.Prefixes;

[UsedImplicitly]
public sealed class SignatureTypePrefix : CodePrefix
{
    protected override string PrefixInner => ";==";
    protected internal override bool AddSemicolon => false;
    protected override async Task HandleInner(TSPlayer Sender, string Code,
                                              ScriptOptions Options, Globals Globals) =>
        await CSharpScript.RunAsync($"return typeof({Code});", Options, Globals)
                          .ContinueWith(s => ((Type)s.Result.ReturnValue)
                                             .GetMembers()
                                             .ForEach(m => Globals.cw(m)));
}