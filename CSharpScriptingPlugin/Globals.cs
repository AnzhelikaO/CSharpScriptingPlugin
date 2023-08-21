namespace CSharpScripting.Configuration;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public record Globals
{
    #region me

    private TSPlayer _me;
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

    public static TSPlayer[] admins =>
        TShock.Players
              .Where(p => (p?.HasPermission(Permissions.USE) is true))
              .Append(server)
              .ToArray();

    #endregion
    [PublicAPI]public static TSPlayer players => TSPlayer.All;
    [PublicAPI]public static TSPlayer server => TSPlayer.Server;
    [PublicAPI]public static readonly TSPlayer[] all = new[] { players, server };

    [PublicAPI]public static readonly TransformDictionaryD varr, vlist, vtuple;
    [PublicAPI]public readonly DynamicDictionary kv = new();
    #region [.].Constructor

    static Globals()
    {
        varr = (d => d.Values.ToArray());
        vlist = (d => d.Values.ToList());
        vtuple = (d => CreateTuple(d.Values.ToArray()));

        #region CreateTuple

        static dynamic CreateTuple(IReadOnlyList<dynamic?> Elements)
        {
            const int MAX_TUPLE_ELEMENTS = 8;
            List<List<dynamic>> elements = new() { new(MAX_TUPLE_ELEMENTS) };
            #region Create

            dynamic Create(int Index) =>
                (elements[Index].Count switch
                {
                    1 => typeof(ValueTuple<>),
                    2 => typeof(ValueTuple<,>),
                    3 => typeof(ValueTuple<,,>),
                    4 => typeof(ValueTuple<,,,>),
                    5 => typeof(ValueTuple<,,,,>),
                    6 => typeof(ValueTuple<,,,,,>),
                    7 => typeof(ValueTuple<,,,,,,>),
                    8 => typeof(ValueTuple<,,,,,,,>),
                    _ => throw new InvalidOperationException()
                })
                .MakeGenericType(elements[Index].Select(e => (Type)e.GetType()).ToArray())
                .GetConstructors()[0]
                .Invoke(elements[Index].ToArray());

            #endregion

            for (int i = 0, a = 1; i < Elements.Count; i++, a++)
            {
                if (a >= MAX_TUPLE_ELEMENTS)
                {
                    elements.Add(new(MAX_TUPLE_ELEMENTS));
                    a = 1;
                }
                elements[^1].Add(Elements[i]);
            }
            for (int i = (elements.Count - 1); i >= 1; i--)
                elements[i - 1].Add(Create(i));
            return Create(0);
        }

        #endregion
    }
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
                case IEnumerable manyReceivers:
                    foreach (object? obj in manyReceivers)
                        if (obj is TSPlayer singleReceiver)
                            receivers.Add(singleReceiver);
                    break;
            }
        if (receivers.Contains(players) && me.RealPlayer)
            receivers.Remove(me);
        return receivers;
    }

    #endregion
}