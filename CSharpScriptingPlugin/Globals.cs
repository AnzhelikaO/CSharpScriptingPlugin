#region Using

using TShockAPI;
// ReSharper disable InconsistentNaming

#endregion
namespace CSharpScriptingPlugin.Configuration;

public record Globals
{
    #region me
    
    internal TSPlayer? _me;
    public TSPlayer me
    {
        get => (_me ?? throw new InvalidOperationException(
                            $"Property '{nameof(me)}' was not initialized."));
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _me = value;
        }
    }

    #endregion
    #region admins

    [PublicAPI]
    public static TSPlayer[] admins =>
        TShock.Players
              .Where(p => ((p is not null) && p.HasPermission(Permissions.USE)))
              .ToArray();

    #endregion
    [PublicAPI]public static TSPlayer all => TSPlayer.All;
    [PublicAPI]public static TSPlayer server => TSPlayer.Server;

    [PublicAPI]public static readonly AsValuesArray varr = new();
    [PublicAPI]public static readonly AsValuesList vlist = new();
    [PublicAPI]public static readonly AsValuesTuple vtuple = new();

    [PublicAPI]public readonly DynamicDictionary kv = new();

    #region cw

    public virtual void cw(object? Object = default, params object?[]? Receivers)
    {
        string text = ToStringNull(Object);
        foreach (TSPlayer receiver in GetReceivers(Receivers))
            receiver.SendInfoMessage(text);
    }

    #endregion
    #region GetReceivers

    protected HashSet<TSPlayer> GetReceivers(IEnumerable<object?>? Receivers)
    {
        HashSet<TSPlayer> receivers = new() { me };
        if (Receivers is null)
            return receivers;
        
        foreach (object? _receiver in Receivers)
            switch (_receiver)
            {
                case TSPlayer singleReceiver:
                    receivers.Add(singleReceiver);
                    break;
                case IEnumerable<TSPlayer?> manyReceivers:
                    foreach (TSPlayer? singleReceiver in manyReceivers)
                        if (singleReceiver is not null)
                            receivers.Add(singleReceiver);
                    break;
            }
        if (ReferenceEquals(server, me)
                ? receivers.Contains(server)
                : receivers.Contains(all))
            receivers.Remove(me);
        return receivers;
    }

    #endregion
}