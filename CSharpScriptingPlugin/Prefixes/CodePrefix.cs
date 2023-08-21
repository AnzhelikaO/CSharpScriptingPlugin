namespace CSharpScripting.Configuration.Prefixes;

public abstract class CodePrefix
{
    public const string DEFAULT_PREFIX = ";";

    internal virtual bool Register => true;
    protected internal virtual bool AddSemicolon => true;
    protected virtual bool ShowInputToSender => true;
    protected virtual bool ShowInputToAdmins => true;
    #region Prefix[Inner]

    private string? _Prefix;
    public string Prefix => (_Prefix ??= ValidateConstant(PrefixInner, _Prefix));
    protected abstract string PrefixInner { get; }

    #endregion

    #region Handle

    public async Task Handle(TSPlayer Sender, string HandleCode, string? ShowCode = null)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        ArgumentNullException.ThrowIfNull(HandleCode);
        ShowCode ??= HandleCode;

        try
        {
            CodeManager manager = CodeManager.Manager;
            (ScriptOptions options, Globals globals) = manager.PlayerManager.Get(Sender);
            await ShowInput(Sender, ShowCode, manager);
            await HandleInner(Sender, HandleCode, manager, options, globals);
        }
        catch (Exception exception)
        {
            Sender.SendErrorMessage(exception.ToString());
        }
    }

    #endregion

    #region ShowInput[Inner]

    private async Task ShowInput(TSPlayer Sender, string Code, CodeManager CodeManager)
    {
        bool toSender = ShowInputToSender, toAdmins = ShowInputToAdmins;
        if (!toSender && !toAdmins)
            return;

        TSPlayer[] admins = Globals.admins;
        string text = $"{Prefix}{Code}";
        if (toAdmins)
            foreach (TSPlayer plr in admins)
                plr.SendMessage(text, CodeManager.CodeColor);
        if (toSender && (!toAdmins || !admins.Contains(Sender)))
            Sender.SendMessage(text, CodeManager.CodeColor);

        await ShowInputInner(Sender, Code, CodeManager);
    }
    protected virtual Task ShowInputInner(TSPlayer Sender, string Code, CodeManager CodeManager) =>
        Task.CompletedTask;

    #endregion
    #region HandleInner

    protected abstract Task HandleInner(TSPlayer Sender, string Code, CodeManager CodeManager,
                                        ScriptOptions Options, Globals Globals);

    #endregion

    #region Equals

    public override bool Equals(object? Object) =>
        ((Object is CodePrefix prefix)
            && (prefix.GetType() == GetType())
            && (prefix.Register == Register)
            && (prefix.AddSemicolon == AddSemicolon)
            && (prefix.ShowInputToSender == ShowInputToSender)
            && (prefix.ShowInputToAdmins == ShowInputToAdmins)
            && (prefix.Prefix == Prefix));

    #endregion
    #region GetHashCode

    public override int GetHashCode() =>
        HashCode.Combine(Register, AddSemicolon, ShowInputToSender, ShowInputToAdmins, Prefix);

    #endregion
    #region ToString

    public override string ToString() => Prefix;

    #endregion
}