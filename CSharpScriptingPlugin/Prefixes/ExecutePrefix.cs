﻿namespace CSharpScripting.Configuration.Prefixes;

[UsedImplicitly]
public sealed class ExecutePrefix : CodePrefix
{
    private const string USING = "using";
    private static readonly Regex USING_REGEX = new(@$"^using\s+(?<{USING}>[^;]+);?$");
    protected override string PrefixInner => DEFAULT_PREFIX;

    protected override async Task HandleInner(TSPlayer Sender, string Code, CodeManager CodeManager,
                                              ScriptOptions Options, Globals Globals)
    {
        await CSharpScript.RunAsync(Code, Options, Globals);
        Match @using = USING_REGEX.Match(Code);
        if (@using.Success)
            CodeManager.PlayerManager.Options.Set(Sender, Options.AddImports(@using.Groups[USING].Value));
    }
}