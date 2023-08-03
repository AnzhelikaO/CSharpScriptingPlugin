// ReSharper disable InconsistentNaming
namespace CSharpScriptingPlugin.Configuration;

public sealed class DynamicDictionary
{
    #region Records

    private sealed record Null { public static readonly Null Instance = new(); }
    private sealed record Val(dynamic Value, ulong Index);

    #endregion

    private static ulong Index;
    private readonly Dictionary<dynamic, Val> InnerDictionary = new();
    public event GetSetValueD? OnGetValue, OnSetValue;
    public event ClearD? OnClear, OnRemoveEvents;
    #region .Constructor

    internal DynamicDictionary() { }

    #endregion

    #region operator[] (Single key)
    
    [PublicAPI]
    public dynamic? this[dynamic? Key]
    {
        get
        {
            dynamic? val = (InnerDictionary.TryGetValue((Key ?? Null.Instance), out Val? value)
                                ? value
                                : null)?.Value;
            return ((OnGetValue is GetSetValueD onGetValue) ? onGetValue(Key, val) : val);
        }
        set
        {
            dynamic? val = value;
            if (OnSetValue is GetSetValueD onSetValue)
                val = onSetValue(Key, val);
            InnerDictionary[Key ?? Null.Instance] = new Val(val, ++Index);
        }
    }

    #endregion
    #region operator[] (Multiple keys)

    [PublicAPI]
    public dynamic? this[dynamic? Key1, dynamic? Key2, params dynamic?[]? OtherKeys]
    {
        get
        {
            OtherKeys ??= Array.Empty<dynamic>();
            Dictionary<dynamic, dynamic> dict = new(OtherKeys.Length + 2);
            foreach (dynamic? key in new[] { Key1, Key2 }.Concat(OtherKeys))
                dict[(key ?? Null.Instance)] = this[key];
            return dict;
        }
        set
        {
            OtherKeys ??= Array.Empty<dynamic>();
            if (!ExtractValues(value, out IEnumerable<dynamic> dynamics))
            {
                foreach (dynamic? key in new[] { Key1, Key2 }.Concat(OtherKeys))
                    this[(key ?? Null.Instance)] = value;
                return;
            }

            dynamic[] val = dynamics.ToArray();
            if (val.Length != (OtherKeys.Length + 2))
                throw new ArgumentOutOfRangeException(
                    $"{val.Length} values for {OtherKeys.Length + 2} keys.");
            this[(Key1 ?? Null.Instance)] = val[0];
            this[(Key2 ?? Null.Instance)] = val[1];
            for (int i = 0; i < OtherKeys.Length; i++)
                this[(OtherKeys[i] ?? Null.Instance)] = val[i + 2];
        }
    }

    #endregion
    #region operator[] (Multiple keys, transform values)

    [PublicAPI]
    public dynamic? this[As? As, dynamic? Key1, dynamic? Key2, params dynamic?[]? OtherKeys]
    {
        get
        {
            Dictionary<dynamic, dynamic> dict = this[Key1, Key2, OtherKeys];
            return ((As is null) ? dict : As.Transform(dict));
        }
    }

    #endregion

    #region Clear

    public bool Clear(bool Force = false)
    {
        bool clear = DoClear(OnClear, Force);
        if (clear)
            InnerDictionary.Clear();
        return clear;
    }

    #endregion
    #region RemoveEvents
    
    public bool RemoveEvents(bool Force = false)
    {
        bool clear = DoClear(OnRemoveEvents, Force);
        if (clear)
            OnGetValue = OnSetValue = null;
        return clear;
    }

    #endregion
    #region Reset

    [PublicAPI]
    public bool Reset(bool ClearDictionary = true, bool ClearEvents = true, bool Force = false) =>
        ((!ClearDictionary || Clear(Force)) && (!ClearEvents || RemoveEvents(Force)));

    #endregion

    #region Show

    #region [Summary]

    /// <summary>
    ///     Equals to <see cref="Show"/>
    /// </summary>

    #endregion
    [PublicAPI]
    public string s(bool All = false) => Show(All);
    public string Show(bool All = false) =>
        string.Join("\n", (All
                               ? InnerDictionary.Values
                               : InnerDictionary.Values.Where(v => (Index - v.Index) <= 10))
                          .OrderBy(v => v.Index)
                          .Select(v => $"[{v.Index}] {ToStringNull(v.Value)}"));

    #endregion
    #region ToString

    public override string ToString() => Show(All: false);

    #endregion

    #region DoClear

    private static bool DoClear(ClearD? Event, bool Force) =>
        ((Event is null) || Event(Force) || Force);

    #endregion
}