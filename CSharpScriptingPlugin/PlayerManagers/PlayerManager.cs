namespace CSharpScripting.Configuration.PlayerManagers;

public abstract class PlayerManager<T>
{
    public virtual string DataKey => string.Empty;
    #region .Constructor

    private protected PlayerManager() { }

    #endregion

    #region Get[Default]

    public virtual T Get(TSPlayer Sender)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        lock (Sender)
        {
            if (!Sender.ContainsData(DataKey))
                Sender.SetData(DataKey, GetDefault(Sender));
            return Sender.GetData<T>(DataKey);
        }
    }
    protected abstract T GetDefault(TSPlayer Sender);

    #endregion
    #region Set

    public virtual void Set(TSPlayer Sender, T Data)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        ArgumentNullException.ThrowIfNull(Data);

        lock (Sender)
            Sender.SetData(DataKey, Data);
    }

    #endregion
}
public sealed class PlayerManager : PlayerManager<PlayerInfo>
{
    public ScriptOptionsPlayerManager Options { get; } = new();
    public GlobalsPlayerManager Globals { get; } = new();
    #region .Constructor

    internal PlayerManager() { }

    #endregion

    #region Get

    public override PlayerInfo Get(TSPlayer Sender)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        lock (Sender)
            return new(Options.Get(Sender), Globals.Get(Sender));
    }
    protected override PlayerInfo GetDefault(TSPlayer Sender) => default;

    #endregion
    #region Set

    public override void Set(TSPlayer Sender, PlayerInfo Data)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        ArgumentNullException.ThrowIfNull(Data);
        ArgumentNullException.ThrowIfNull(Data.Options);
        ArgumentNullException.ThrowIfNull(Data.Globals);

        lock (Sender)
        {
            Options.Set(Sender, Data.Options);
            Globals.Set(Sender, Data.Globals);
        }
    }

    #endregion
}