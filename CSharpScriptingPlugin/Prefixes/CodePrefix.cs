namespace CSharpScripting.Configuration.Prefixes;

public abstract class CodePrefix
{
    public const string DEFAULT_PREFIX = ";";

    internal virtual bool Register => true;
    protected internal virtual bool AddSemicolon => true;
    #region Prefix[Inner]

    private string? _Prefix;
    public string Prefix => (_Prefix ??= ValidateConstant(PrefixInner, _Prefix));
    protected abstract string PrefixInner { get; }

    #endregion

    #region Handle

    public async Task Handle(TSPlayer Sender, CSEnvironment Environment,
                             string HandleCode, string? ShowCode = null)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        ArgumentNullException.ThrowIfNull(Environment);
        ArgumentNullException.ThrowIfNull(HandleCode);
        ShowCode ??= HandleCode;

        try
        {
            CodeManager manager = CodeManager.Manager;
            (string @using, ScriptOptions options, Globals globals) = Environment;
            await ShowInput(Sender, Environment, ShowCode, manager, options, globals);
            await HandleInner(Sender, Environment, @using, HandleCode, manager, options, globals);
        }
        catch (Exception exception)
        {
            Sender.SendErrorMessage(exception.ToString());
        }
    }

    #endregion

    #region ShowInput[Inner]

    [SuppressMessage("ReSharper", "EmptyGeneralCatchClause")]
    private async Task ShowInput(TSPlayer Sender, CSEnvironment Environment, string Code,
                                 CodeManager CodeManager, ScriptOptions Options, Globals Globals)
    {
        try { await ShowInputInner(Sender, Environment, Code, CodeManager, Options, Globals); }
        catch { }
    }
    protected virtual Task ShowInputInner(TSPlayer Sender, CSEnvironment Environment, string Code,
                                          CodeManager CodeManager, ScriptOptions Options, Globals Globals)
    {
        string text = $"{Prefix}{Code}";
        foreach (TSPlayer plr in GetShowInputPlayers(Sender, Environment, CodeManager, Options, Globals))
            plr.SendMessage(text, CodeManager.CodeColor);
        return Task.CompletedTask;
    }

    #endregion
    #region GetShowInputPlayers[Inner]

    protected IEnumerable<TSPlayer> GetShowInputPlayers(
        TSPlayer Sender, CSEnvironment Environment, CodeManager CodeManager,
        ScriptOptions Options, Globals Globals)
    {
        if (GetShowInputPlayersInner(Sender, Environment, CodeManager, Options,
                                     Globals) is IEnumerable<TSPlayer?> players)
            foreach (TSPlayer? player in players)
                if (player is not null)
                    yield return player;
    }
    protected virtual IEnumerable<TSPlayer> GetShowInputPlayersInner(
            TSPlayer Sender, CSEnvironment Environment, CodeManager CodeManager,
            ScriptOptions Options, Globals Globals) =>
        Globals.admins.Append(Globals.me);

    #endregion
    #region HandleInner

    protected abstract Task HandleInner(TSPlayer Sender, CSEnvironment Environment,
                                        string Using, string Code, CodeManager CodeManager,
                                        ScriptOptions Options, Globals Globals);

    #endregion

    #region Equals, GetHashCode, ToString

    public override bool Equals(object? Object) =>
        ((Object is CodePrefix prefix)
            && (prefix.GetType() == GetType())
            && (prefix.Register == Register)
            && (prefix.AddSemicolon == AddSemicolon)
            && (prefix.Prefix == Prefix));
    public override int GetHashCode() => HashCode.Combine(Register, AddSemicolon, Prefix);
    public override string ToString() => Prefix;

    #endregion
}