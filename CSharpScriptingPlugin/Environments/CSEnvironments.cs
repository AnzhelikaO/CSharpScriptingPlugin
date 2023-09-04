namespace CSharpScripting.Environments;

#warning TODO: environments hooks
public sealed class CSEnvironments
{
    #region [Non]InGameEnvironments

    private readonly SortedDictionary<int, CSEnvironment?> InGameEnvironments =
        new(Enumerable.Range(0, Main.maxPlayers)
                      .ToDictionary(i => i, _ => (CSEnvironment?)null));
    private readonly SortedDictionary<TSPlayer, CSEnvironment?> NonInGameEnvironments = new();

    #endregion
    #region Lock

    private readonly object[] Lock = Enumerable.Range(0, Main.maxPlayers)
                                               .Select(_ => new object())
                                               .ToArray();

    #endregion
    [PublicAPI]public CSEnvironment Server => this[TSPlayer.Server];
    #region .Constructor

    internal CSEnvironments() { }

    #endregion

    internal CSEnvironment this[long Token] => this[Tokens.Taken[Token]];
    [AllowNull]
    public CSEnvironment this[TSPlayer Player]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(Player);
            if (Player.RealPlayer)
                lock (Lock[ValidateIndex(Player.Index)])
                {
                    if (InGameEnvironments[Player.Index] is not CSEnvironment env)
                        env = InGameEnvironments[Player.Index] = new(Player);
                    return env;
                }
            else
                lock (Player)
                {
                    if (!NonInGameEnvironments.TryGetValue(Player, out CSEnvironment? env) || (env is null))
                        env = NonInGameEnvironments[Player] = new(Player);
                    return env;
                }
        }
        set
        {
            ArgumentNullException.ThrowIfNull(Player);
            if (value is null)
                Tokens.Release(Player);

            if (Player.RealPlayer)
                lock (Lock[ValidateIndex(Player.Index)])
                    InGameEnvironments[Player.Index] = value;
            else
                lock (Player)
                    NonInGameEnvironments[Player] = value;
        }
    }
    #region ValidateIndex

    [SuppressMessage("ReSharper", "MergeIntoLogicalPattern")]
    private static int ValidateIndex(int Index, [CallerArgumentExpression(nameof(Index))]string Name = "")
    {
        if ((Index < 0) || (Index >= Main.maxPlayers))
            throw new ArgumentOutOfRangeException(
                Name, Index, $"If !{nameof(TSPlayer)}.{nameof(TSPlayer.RealPlayer)}, then " +
                             $"{nameof(TSPlayer)}.{nameof(TSPlayer.Index)} must be greater than " +
                             $"or equal to 0 and less than {Main.maxPlayers}, {Index} given.");
        return Index;
    }

    #endregion
}