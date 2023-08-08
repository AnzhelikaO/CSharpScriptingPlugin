#region Using

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using CSharpScripting.Configuration.Delegates;

#endregion
namespace CSharpScripting.Configuration.Prefixes;

public sealed class CodePrefixesCollection
    : IReadOnlyDictionary<string, CodePrefix>,
      IReadOnlyList<KeyValuePair<string, CodePrefix>>,
      IReadOnlyList<CodePrefix>,
      IReadOnlyCollection<KeyValuePair<string, CodePrefix>>,
      IEnumerable<KeyValuePair<string, CodePrefix>>,
      IEnumerable<CodePrefix>,
      IEnumerable
{
    private readonly ConcurrentDictionary<string, CodePrefix> _Prefixes = new();
    public RegisterCodePrefixD? OnRegister;
    public DeregisterCodePrefixD? OnDeregister;
    public IEnumerable<CodePrefix> Sorted => _Prefixes.Values.OrderByDescending(p => p.Prefix.Length);
    public int Count => _Prefixes.Count;
    #region .Constructor
    // ReSharper disable once MergeIntoPattern

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

    // ReSharper restore MergeIntoPattern
    #endregion

    #region Register

    public bool Register([MaybeNull]CodePrefix Prefix)
    {
        ArgumentNullException.ThrowIfNull(Prefix);
        if (OnRegister is RegisterCodePrefixD onRegister)
            Prefix = onRegister(Prefix);
        return ((Prefix is not null) && _Prefixes.TryAdd(Prefix.Prefix, Prefix));
    }

    #endregion
    #region Deregister

    [PublicAPI]
    public bool Deregister(CodePrefix Prefix)
    {
        ArgumentNullException.ThrowIfNull(Prefix);
        return (((OnDeregister is not DeregisterCodePrefixD onDeregister) || onDeregister(Prefix))
                && _Prefixes.TryRemove(Prefix.Prefix, out _));
    }

    #endregion
    #region Contains

    public bool Contains([NotNullWhen(true)]string? Prefix) =>
        ((Prefix is not null) && _Prefixes.ContainsKey(Prefix));

    #endregion
    #region TryGet

    public bool TryGet([NotNullWhen(true)]string? Prefix, [MaybeNullWhen(false)]out CodePrefix CodePrefix)
    {
        CodePrefix = null;
        return ((Prefix is not null) && _Prefixes.TryGetValue(Prefix, out CodePrefix));
    }
    public bool TryGet([NotNullWhen(true)]string? Input,
                       [MaybeNullWhen(false)]out string Code,
                       [MaybeNullWhen(false)]out CodePrefix CodePrefix)
    {
        (Code, CodePrefix, Input) = (null, null, Input?.Trim());
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

    #region GetEnumerator

    public IEnumerator<CodePrefix> GetEnumerator() => _Prefixes.Values.GetEnumerator();
    IEnumerator<KeyValuePair<string, CodePrefix>>
        IEnumerable<KeyValuePair<string, CodePrefix>>.GetEnumerator() => _Prefixes.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _Prefixes.Values.GetEnumerator();
    
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