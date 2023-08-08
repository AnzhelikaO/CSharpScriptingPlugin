namespace CSharpScripting.Configuration.PlayerManagers;

public abstract class PlayerManager<T>
{
    #region DataKey
    
    public abstract string DataKey { get; }

    #endregion

    #region Get[Default]

    public T Get(TSPlayer Sender)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        if (!Sender.ContainsData(DataKey))
            Sender.SetData(DataKey, GetDefault(Sender));
        return Sender.GetData<T>(DataKey);
    }
    private protected abstract T GetDefault(TSPlayer Sender);

    #endregion
    #region Set

    public void Set(TSPlayer Sender, T Data)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        Sender.SetData(DataKey, Data);
    }

    #endregion
}