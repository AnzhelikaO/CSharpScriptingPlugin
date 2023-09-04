namespace CSharpScripting.TShockPlugin;

public sealed class CSSCommand : Command
{
    public static readonly CSSCommand Instance = new();
    #region .Constructor

    private CSSCommand() : base(Configuration.Permissions.USE, Execute, "css") { }

    #endregion

    private static void Execute(CommandArgs Args)
    {
        switch (Args.Parameters.ElementAtOrDefault(0)?.ToLower())
        {
            case "reload" or "r":
                if (GetTarget(ForReload: true, out bool @using, out _, out bool globals))
                    _ = CodeManager.Manager
                                   .Environments[Args.Player]
                                   .Reload(@using, globals);
                break;
            case "reset":
                if (GetTarget(ForReload: false, out @using, out bool options, out globals))
                    _ = CodeManager.Manager
                                   .Environments[Args.Player]
                                   .Reset(@using, options, globals);
                break;
            default:
                ShowError();
                break;
        }
        
        #region GetTarget

        bool GetTarget(bool ForReload, out bool Using, out bool Options, out bool Globals)
        {
            Using = Options = Globals = false;
            if (Args.Parameters.Count < 2)
                return ShowError(ForReload);

            foreach (string arg in Args.Parameters.Skip(1))
                switch (arg.ToLower())
                {
                    case "using" or "u":
                        Using = true; break;
                    case "options" or "o":
                        if (ForReload)
                            return ShowReloadError();
                        Options = true;
                        break;
                    case "globals" or "g":
                        Globals = true; break;
                    case "all" or "a":
                        Using = Options = Globals = true; break;
                    default:
                        return ShowError(ForReload);
                }
            return true;
        }

        #endregion
        #region Show[Reload/Reset]Error
        
        bool ShowError(bool? ForReload = null)
        {
            if (ForReload is bool forReload)
                return (forReload ? ShowReloadError() : ShowResetError());

            ShowReloadError();
            ShowResetError();
            return false;
        }
        bool ShowReloadError()
        {
            Args.Player.SendErrorMessage("/css <reload/r> [using/u] [globals/g]");
            Args.Player.SendErrorMessage("/css <reload/r> all");
            return false;
        }
        bool ShowResetError()
        {
            Args.Player.SendErrorMessage("/css reset [using/u] [options/o] [globals/g]");
            Args.Player.SendErrorMessage("/css reset all");
            return false;
        }

        #endregion
    }
}