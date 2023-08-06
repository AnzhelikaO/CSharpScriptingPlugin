#region Using

global using CSharpScriptingPlugin.Configuration;
global using JetBrains.Annotations;
global using static CSharpScriptingPlugin.Configuration.Helpers;
using System.Diagnostics;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

#endregion
namespace CSharpScriptingPlugin.Plugin;

[ApiVersion(2, 1)]
[PublicAPI]
public sealed class Plugin : TerrariaPlugin
{
    #region [My]AssemblyInfo

    private sealed record AssemblyInfo(string Name, string Author, Version Version, string Description);

    private AssemblyInfo? _MyAssemblyInfo;
    private AssemblyInfo MyAssemblyInfo
    {
        get
        {
            if (_MyAssemblyInfo is null)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                _MyAssemblyInfo =
                    new(assembly.GetCustomAttribute<AssemblyTitleAttribute>()!.Title,
                        assembly.GetCustomAttribute<AssemblyCompanyAttribute>()!.Company,
                        Version.Parse(assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version),
                        assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()!.Description);
            }
            return _MyAssemblyInfo;
        }
    }

    #endregion
    public override string Name => MyAssemblyInfo.Name;
    public override string Author => MyAssemblyInfo.Author;
    public override Version Version => MyAssemblyInfo.Version;
    public override string Description => MyAssemblyInfo.Description;
    #region .Constructor

    public Plugin(Main Game) : base(Game) { }

    #endregion

    #region [OnPost]Initialize

    public override void Initialize()
    {
        ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
        ServerApi.Hooks.ServerChat.Register(this, OnServerChat);
        ServerApi.Hooks.ServerCommand.Register(this, OnServerCommand);
    }
    private void OnPostInitialize(EventArgs Args) =>
        CodeManager.Manager?.Initialize();

    #endregion
    #region Dispose

    protected override void Dispose(bool Disposing)
    {
        if (Disposing)
        {
            ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
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