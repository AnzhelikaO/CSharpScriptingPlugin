namespace CSharpScripting.Configuration.PlayerManagers;

public sealed class GlobalsPlayerManager : PlayerManager<Globals>
{
    public override string DataKey => $"{nameof(CSharpScripting)}_{nameof(Globals)}_Data";
    private protected override Globals GetDefault(TSPlayer Sender) => new(Sender);
}