namespace CSharpScripting.Configuration.PlayerManagers;

public sealed class ScriptOptionsPlayerManager : PlayerManager<ScriptOptions>
{
    public override string DataKey => $"{nameof(CSharpScripting)}_{nameof(ScriptOptions)}_Data";
    #region .Constructor

    internal ScriptOptionsPlayerManager() { }

    #endregion

    protected override ScriptOptions GetDefault(TSPlayer Sender) => CodeManager.DefaultOptions;
}