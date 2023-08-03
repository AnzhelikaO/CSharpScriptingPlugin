#region Using

global using CSharpScriptingPlugin.Configuration;
global using JetBrains.Annotations;
global using static CSharpScriptingPlugin.Configuration.Helpers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

#endregion
namespace CSharpScriptingPlugin.Plugin;

[ApiVersion(2, 1)]
[PublicAPI]
public sealed class Plugin : TerrariaPlugin
{
    public Plugin(Main Game) : base(Game) { }

    #region Initialize

    public override void Initialize()
    {
        CodeManager.Manager?.Initialize();
        ServerApi.Hooks.ServerChat.Register(this, OnServerChat);
        ServerApi.Hooks.ServerCommand.Register(this, OnServerCommand);
    }

    #endregion
    #region Dispose

    protected override void Dispose(bool Disposing)
    {
        if (Disposing)
        {
            ServerApi.Hooks.ServerChat.Deregister(this, OnServerChat);
            ServerApi.Hooks.ServerCommand.Deregister(this, OnServerCommand);
        }
        base.Dispose(Disposing);
    }

    #endregion

    #region OnServerChat

    private void OnServerChat(ServerChatEventArgs Args) =>
        Args.Handled = (Args.Handled || HandleInput(TShock.Players[Args.Who], Args.Text));

    #endregion
    #region OnServerCommand

    private void OnServerCommand(CommandEventArgs Args) =>
        Args.Handled = (Args.Handled || HandleInput(TSPlayer.Server, Args.Command));

    #endregion

    #region HandleInput

    private static bool HandleInput(TSPlayer? Sender, string Text)
    {
        if ((Sender is null)
                || string.IsNullOrWhiteSpace(Text)
                || (CodeManager.Manager is not CodeManager manager)
                || !manager.ShouldHandle(Sender, Text, out string? prefix, out string? code))
            return false;

        _ = manager.Handle(Sender, prefix, code);
        return true;
    }

    #endregion
}