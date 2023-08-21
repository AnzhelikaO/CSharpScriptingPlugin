namespace CSharpScripting.Configuration;

public sealed class DynamicDictionary
{
    #region Records

    private sealed record Val(dynamic Value, ulong Index);

    #endregion
    #region EventsCollection

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class EventsCollection
    {
        public event GetValuesD? GetValues;
        public event SetValuesD? SetValues;
        public event PreClearDictionaryD? PreClear, PreRemoveEvents;
        public event PostClearDictionaryD? PostClear, PostRemoveEvents;

        internal ref GetValuesD? _GetValues => ref GetValues;
        internal ref SetValuesD? _SetValues => ref SetValues;
        internal PreClearDictionaryD? _PreClear => PreClear;
        internal PreClearDictionaryD? _PreRemoveEvents => PreRemoveEvents;
        internal PostClearDictionaryD? _PostClear => PostClear;
        internal PostClearDictionaryD? _PostRemoveEvents => PostRemoveEvents;

        internal EventsCollection() { }
    }

    #endregion

    private ulong Index;
    private readonly Dictionary<dynamic, Val> InnerDictionary = new();
    public EventsCollection Events { get; } = new();
    #region .Constructor

    internal DynamicDictionary() { }

    #endregion

    #region operator[]
#pragma warning disable CS8619 // dynamic does not work well with nullability

    public dynamic? this[params dynamic?[]? Keys]
    {
        get
        {
            ExtractKeys(Keys, out TransformDictionaryD? transform, out dynamic?[] keys);
            Dictionary<dynamic, dynamic?> kv =
                keys.ToDictionary(k => (k ?? Null.Instance),
                                  k => (InnerDictionary.TryGetValue((k ?? Null.Instance), out Val? val)
                                            ? val?.Value
                                            : null));
            dynamic? ret = ((transform is null) ? kv : transform.Invoke(kv));
            Events._GetValues?.Invoke(transform, keys, kv, ref ret);
            return ((ret is IDictionary { Count: 1 } dict)
                        ? dict.Values.Cast<dynamic>().First()
                        : ret);

            #region ExtractKeys

            static void ExtractKeys(dynamic?[]? KeysIn, out TransformDictionaryD? Transform,
                                    out dynamic?[] KeysOut)
            {
                KeysIn ??= Array.Empty<dynamic>();
                Transform = (KeysIn.FirstOrDefault() as TransformDictionaryD);
                KeysOut = ((Transform is null)
                               ? KeysIn
                               : ((KeysIn.Length > 1)
                                    ? KeysIn.Skip(1).ToArray()
                                    : throw new ArgumentException(
                                          "Single key is dictionary transformer, no other keys provided.")));
            }

            #endregion
        }
        set
        {
            ExtractKeys(ref Keys);
            dynamic?[] values = new [] { value };
            if (Keys.Length > 1)
                ExtractValues(value, out values);
            if (Keys.Length != values.Length)
                throw new ArgumentException($"{values.Length} values for {Keys.Length} keys.");

            if (Events._SetValues?.Invoke(Keys, values) is not false)
                for (int i = 0; i < Keys.Length; i++)
                    InnerDictionary[Keys[i] ?? Null.Instance] =
                        new Val(values[i], Interlocked.Increment(ref Index));

            #region ExtractKeys

            static void ExtractKeys([NotNull]ref dynamic?[]? Keys) =>
                Keys ??= Array.Empty<dynamic>();

            #endregion
            #region ExtractValues

            static void ExtractValues(dynamic? Value, out dynamic?[] Values) =>
                Values = (Value switch
                {
                    ITuple tuple => ExtractTuple(tuple),
                    dynamic?[] values => values,
                    IEnumerable<dynamic?> values => values.ToArray(),
                    IEnumerable values => values.Cast<dynamic?>().ToArray(),
                    _ => new [] { Value }
                });
            #region ExtractTuple

            static dynamic?[] ExtractTuple(ITuple Tuple)
            {
                dynamic?[] elements = new dynamic?[Tuple.Length];
                for (int i = 0; i < Tuple.Length; i++)
                    elements[i] = Tuple[i];
                return elements;
            }

            #endregion

            #endregion
        }
    }

#pragma warning restore CS8619
    #endregion

    #region Clear

    public bool Clear(bool Force = false)
    {
        bool success = ((Events._PreClear is not PreClearDictionaryD onClear) || onClear(Force) || Force);
        if (success)
            InnerDictionary.Clear();
        Events._PostClear?.Invoke(success, Force);
        return success;
    }

    #endregion
    #region RemoveEvents

    public bool RemoveEvents(bool Force = false)
    {
        bool success = ((Events._PreRemoveEvents is not PreClearDictionaryD onClear)
                            || onClear(Force) || Force);
        if (success)
            (Events._GetValues, Events._SetValues) = (null, null);
        Events._PostRemoveEvents?.Invoke(success, Force);
        return success;
    }

    #endregion
    #region Reset

    [PublicAPI]
    public bool Reset(bool ClearDictionary = true, bool ClearEvents = true, bool Force = false) =>
        ((!ClearDictionary || Clear(Force)) && (!ClearEvents || RemoveEvents(Force)));

    #endregion

    #region Show

    public string Show(bool All = false) =>
        string.Join(
            "\n", (All
                       ? InnerDictionary
                       : InnerDictionary.Where(p => (Index - p.Value.Index) <= 10))
                  .OrderBy(p => p.Value.Index)
                  .Select(p => $"[{p.Value.Index}] {ToStringNull(p.Key)}={ToStringNull(p.Value.Value)}"));

    #endregion
    #region ToString

    public override string ToString() => Show(All: false);

    #endregion
}