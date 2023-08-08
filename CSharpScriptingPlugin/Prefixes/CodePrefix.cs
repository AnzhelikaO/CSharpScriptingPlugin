#region Using

using Microsoft.Xna.Framework;

#endregion
namespace CSharpScripting.Configuration.Prefixes;

public abstract class CodePrefix
{
    #region Register

    internal virtual bool Register => true;

    #endregion
    #region AddSemicolonIfNeeded

    protected internal virtual bool AddSemicolon => true;

    #endregion
    #region Prefix[Inner]

    private string? _Prefix;
    public string Prefix
    {
        get
        {
            ValidateConstant(ref _Prefix, PrefixInner);
            return _Prefix;
        }
    }
    protected abstract string PrefixInner { get; }

    #endregion

    #region Handle

    public async Task Handle(TSPlayer Sender, string ShowCode, string HandleCode)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        ArgumentNullException.ThrowIfNull(ShowCode);
        ArgumentNullException.ThrowIfNull(HandleCode);

        try
        {
            await ShowInput(Sender, ShowCode);
            await HandleInner(Sender, HandleCode,
                              CodeManager.Manager.Options.Get(Sender),
                              CodeManager.Manager.Globals.Get(Sender));
        }
        catch (Exception exception)
        {
            Sender.SendErrorMessage(exception.ToString());
        }
    }

    #endregion

    #region ShowInput

    protected virtual Task ShowInput(TSPlayer Sender, string Code)
    {
        TSPlayer[] admins = Globals.admins;
        string text = $"{Prefix}{Code}";
        foreach (TSPlayer plr in admins)
            plr.SendMessage(text, Color.HotPink);
        if (!admins.Contains(Sender))
            Sender.SendMessage(text, Color.HotPink);
        return Task.CompletedTask;
    }

    #endregion
    #region HandleInner
    
    protected abstract Task HandleInner(TSPlayer Sender, string Code,
                                        ScriptOptions Options, Globals Globals);

    #endregion
}