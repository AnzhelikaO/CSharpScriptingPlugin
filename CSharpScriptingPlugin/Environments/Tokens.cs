namespace CSharpScripting.Environments;

#region [Summary]

/// <summary>
///     So it's a hack to find previous <see cref="Globals"/>. <br/>
///     We cannot take instance field like <see cref="Globals.me"/> and put into new globals
///         type code in the script, thus it's pointless to store current globals. <br/>
///     What we can do instead, is pass some value into script code as plain text
///         since we know current environment in <see cref="CSEnvironment.Reload"/>.
/// </summary>

#endregion
internal static class Tokens
{
    public static readonly Dictionary<long, TSPlayer> Taken = new(Main.maxPlayers);

    public static long Take(TSPlayer Player)
    {
        lock (Taken)
        {
            long token;
            do { token = Random.Shared.NextInt64(long.MinValue, long.MaxValue); }
            while (!Taken.ContainsKey(token));
            Taken.Add(token, Player);
            return token;
        }
    }
    public static void Release(TSPlayer Player)
    {
        lock (Taken)
            foreach (long token in Taken.Where(p => ReferenceEquals(Player, p.Value))
                                        .Select(p => p.Key)
                                        .ToArray())
                Taken.Remove(token);
    }
}