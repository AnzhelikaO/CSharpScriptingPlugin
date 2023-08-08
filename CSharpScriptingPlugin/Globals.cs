#region Using

using CSharpScripting.Configuration.Delegates;
using Microsoft.Xna.Framework;

// ReSharper disable InconsistentNaming

#endregion
namespace CSharpScripting.Configuration;

public record Globals
{
    #region me
    
    internal TSPlayer _me;
    public TSPlayer me
    {
        get => _me;
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
              .Append(server)
              .ToArray();

    #endregion
    [PublicAPI]public static TSPlayer all => TSPlayer.All;
    [PublicAPI]public static TSPlayer server => TSPlayer.Server;

    [PublicAPI]public static readonly TransformDictionaryD varr = (d => d.Values.ToArray());
    [PublicAPI]public static readonly TransformDictionaryD vlist = (d => d.Values.ToList());
    [PublicAPI]public static readonly TransformDictionaryD vtuple =
        (d => CreateValueTuple(d.Values.ToArray()));

    [PublicAPI]public readonly DynamicDictionary kv = new();
    #region .Constructor
    
    public Globals(TSPlayer Me)
    {
        ArgumentNullException.ThrowIfNull(Me);
        _me = Me;
    }

    #endregion

    #region cw

    public virtual void cw(object? Object = default, params object?[]? Receivers)
    {
        string text = ToStringNull(Object);
        foreach (TSPlayer receiver in GetReceivers(Receivers))
            receiver.SendMessage(text, Color.HotPink);
    }

    #endregion
    #region GetReceivers

    protected HashSet<TSPlayer> GetReceivers(object?[]? Receivers)
    {
        HashSet<TSPlayer> receivers = new() { me };
        if ((Receivers is null) || !Receivers.Any())
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