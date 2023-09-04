namespace CSharpScripting.Configuration;

[PublicAPI, SuppressMessage("ReSharper", "InconsistentNaming")]
public class Globals
{
    #region me

    private TSPlayer _me = null!;
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
    public static TSPlayer players => TSPlayer.All;
    public static TSPlayer server => TSPlayer.Server;
    public static readonly TSPlayer[] all = new[] { players, server };

    public static readonly TransformDictionaryD varr, vlist, vtuple;
    public DynamicDictionary kv { get; private set; } = new();
    internal MetadataReference? FromScriptReference;
    #region ..Constructor

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
        }

        #endregion
    }

    #endregion
    #region .Constructor

    protected Globals() { }
    public Globals(TSPlayer Me) : this()
    {
        ArgumentNullException.ThrowIfNull(Me);
        _me = Me;
    }
    public Globals(Globals Base) : this() => CopyFrom(Base);

    #endregion

    #region CopyFrom
    
    protected void CopyFrom(Globals Base)
    {
        ArgumentNullException.ThrowIfNull(Base);
        (_me, kv) = (Base._me, Base.kv);
    }

    #endregion
    #region CopyFromPrevious

    internal const string COPY_FROM_PREVIOUS = nameof(CopyFromPrevious);
    protected void CopyFromPrevious(long Token) =>
        CopyFrom(CodeManager.Manager.Environments[Token].GlobalsNoLock);

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

    public override string ToString() => $"{nameof(me)}={me.Name}, {nameof(kv)}={kv}";
}