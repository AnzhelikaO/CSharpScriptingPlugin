﻿namespace CSharpScripting.Configuration.Prefixes;

public sealed class CodePrefixesCollection
    : IReadOnlyDictionary<string, CodePrefix>,
      IReadOnlyList<KeyValuePair<string, CodePrefix>>,
      IReadOnlyList<CodePrefix>,
      IReadOnlyCollection<KeyValuePair<string, CodePrefix>>,
      IEnumerable<KeyValuePair<string, CodePrefix>>,
      IEnumerable<CodePrefix>,
      IEnumerable
{
    #region InstanceEvents

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class InstanceEvents
    {
        public event PreRegisterCodePrefixD? PreRegister;
        public event PostRegisterCodePrefixD? PostRegister;
        public event PreDeregisterCodePrefixD? PreDeregister;
        public event PostDeregisterCodePrefixD? PostDeregister;

        internal PreRegisterCodePrefixD? _PreRegister => PreRegister;
        internal PostRegisterCodePrefixD? _PostRegister => PostRegister;
        internal PreDeregisterCodePrefixD? _PreDeregister => PreDeregister;
        internal PostDeregisterCodePrefixD? _PostDeregister => PostDeregister;

        internal InstanceEvents() { }
    }

    #endregion

    private readonly ConcurrentDictionary<string, CodePrefix> Prefixes = new();
    public IEnumerable<CodePrefix> Sorted => Prefixes.Values.OrderByDescending(p => p.Prefix.Length);
    public int Count => Prefixes.Count;
    public InstanceEvents Events { get; } = new();
    #region .Constructor

    [SuppressMessage("ReSharper", "MergeIntoPattern")]
    internal CodePrefixesCollection()
    {
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            if (!type.IsAbstract
                    && type.IsSubclassOf(typeof(CodePrefix))
                    && type.GetConstructors(BindingFlags.Public
                                          | BindingFlags.NonPublic
                                          | BindingFlags.Instance)
                           .Any(c => !c.GetParameters().Any())
                    && (Activator.CreateInstance(type, nonPublic: true) is CodePrefix prefix)
                    && prefix.Register)
                Register(prefix);
    }

    #endregion

    #region operator[] (Prefix)

    public CodePrefix this[string Prefix]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(Prefix);
            if (!TryGet(Prefix, out CodePrefix? codePrefix))
                throw new KeyNotFoundException($"Invalid prefix \"{Prefix}\".");
            return codePrefix;
        }
    }

    #endregion
    #region operator[] (Index)

    public CodePrefix this[int Index]
    {
        get
        {
            if (Index < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(Index), Index,
                    $"{nameof(Index)} must be greater than or equal to 0, {Index} given.");
            CodePrefix[] prefixes = Sorted.ToArray();
            if (Index >= prefixes.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(Index), Index,
                    $"{nameof(Index)} must be less than {prefixes.Length}, {Index} given.");
            return prefixes[Index];
        }
    }
    KeyValuePair<string, CodePrefix> IReadOnlyList<KeyValuePair<string, CodePrefix>>.this[int Index]
    {
        get
        {
            CodePrefix prefix = this[Index];
            return new(prefix.Prefix, prefix);
        }
    }

    #endregion

    #region Register

    public bool Register([NotNullWhen(true)]CodePrefix? Prefix)
    {
        if (Prefix is null)
            return false;

        CodePrefix saved = Prefix;
        if (Events._PreRegister is PreRegisterCodePrefixD onRegister)
            Prefix = onRegister(Prefix);
        if ((Prefix is not null) && Prefixes.TryAdd(Prefix.Prefix, Prefix))
        {
            Events._PostRegister?.Invoke(Success: true, Prefix);
            return true;
        }
        else
        {
            Events._PostRegister?.Invoke(Success: false, (Prefix ?? saved));
            return false;
        }
    }

    #endregion
    #region Deregister

    [PublicAPI]
    public bool Deregister([NotNullWhen(true)]CodePrefix? Prefix)
    {
        if (Prefix is null)
            return false;

        bool success = (((Events._PreDeregister is not PreDeregisterCodePrefixD onDeregister)
                                || onDeregister(Prefix))
                            && Prefixes.TryRemove(Prefix.Prefix, out _));
        Events._PostDeregister?.Invoke(success, Prefix);
        return success;
    }

    #endregion
    #region Contains

    public bool Contains([NotNullWhen(true)]string? Prefix) =>
        ((Prefix is not null) && Prefixes.ContainsKey(Prefix));

    #endregion
    #region TryGet

    public bool TryGet([NotNullWhen(true)]string? Prefix, [MaybeNullWhen(false)]out CodePrefix CodePrefix)
    {
        CodePrefix = null;
        return ((Prefix is not null) && Prefixes.TryGetValue(Prefix, out CodePrefix));
    }
    public bool TryGet([NotNullWhen(true)]string? Input,
                       [MaybeNullWhen(false)]out string Code,
                       [MaybeNullWhen(false)]out CodePrefix CodePrefix)
    {
        (Input, Code, CodePrefix) = (Input?.Trim(), null, null);
        if (string.IsNullOrWhiteSpace(Input))
            return false;

        string lowerInput = Input.ToLower();
        CodePrefix = Sorted.FirstOrDefault(p => lowerInput.StartsWith(p.Prefix));
        if (CodePrefix is null)
            return false;

        Code = Input[CodePrefix.Prefix.Length..].Trim();
        return true;
    }

    #endregion

    #region GetEnumerator

    public IEnumerator<CodePrefix> GetEnumerator() => Sorted.GetEnumerator();
    IEnumerator<KeyValuePair<string, CodePrefix>>
        IEnumerable<KeyValuePair<string, CodePrefix>>.GetEnumerator() =>
        Sorted.Select(p => new KeyValuePair<string, CodePrefix>(p.Prefix, p))
              .GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Sorted.GetEnumerator();

    #endregion
    #region Interface realizations

    IEnumerable<string> IReadOnlyDictionary<string, CodePrefix>.Keys => Sorted.Select(p => p.Prefix);
    IEnumerable<CodePrefix> IReadOnlyDictionary<string, CodePrefix>.Values => Sorted;

    bool IReadOnlyDictionary<string, CodePrefix>.ContainsKey(string Prefix) => Contains(Prefix);
    bool IReadOnlyDictionary<string, CodePrefix>.TryGetValue(
            string Prefix, [MaybeNullWhen(false)]out CodePrefix CodePrefix) =>
        TryGet(Prefix, out CodePrefix);

    #endregion
}