namespace CSharpScripting.Configuration.PlayerManagers;

public sealed class ScriptOptionsPlayerManager : PlayerManager<ScriptOptions>
{
    public override string DataKey => $"{nameof(CSharpScripting)}_{nameof(ScriptOptions)}_Data";
    private protected override ScriptOptions GetDefault(TSPlayer Sender) =>
        CodeManager.Manager.DefaultOptions;
}