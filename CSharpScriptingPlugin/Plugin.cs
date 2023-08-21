#region [Global] using

global using CSharpScripting.Configuration;
global using CSharpScripting.Configuration.Delegates;
global using CSharpScripting.Configuration.PlayerManagers;
global using CSharpScripting.Configuration.Prefixes;
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Scripting;
global using Microsoft.CodeAnalysis.Scripting;
global using System.Collections;
global using System.Collections.Concurrent;
global using System.Diagnostics.CodeAnalysis;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Text.RegularExpressions;
global using Terraria;
global using TerrariaApi.Server;
global using TShockAPI;
global using static CSharpScripting.Helpers;
global using Permissions = CSharpScripting.Configuration.Permissions;
global using Null = CSharpScripting.Configuration.SpecialNull.Null;
global using Plugin = CSharpScripting.TShockPlugin.Plugin;
global using PublicAPIAttribute = JetBrains.Annotations.PublicAPIAttribute;
global using UsedImplicitlyAttribute = JetBrains.Annotations.UsedImplicitlyAttribute;
global using Color = Microsoft.Xna.Framework.Color;
using System.ComponentModel;

#endregion
namespace CSharpScripting.TShockPlugin;

[ApiVersion(2, 1)]
public sealed class Plugin : TerrariaPlugin
{
    #region Instance

    private static Plugin? _Instance;
    private static Plugin Instance =>
        (_Instance ?? throw new InvalidOperationException(
                        "Toggling hooks when plugin was not instantiated."));

    #endregion
    #region PluginInfo

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

    #endregion
    #region PropertySetters

    private static readonly Dictionary<Type, MethodInfo> PropertySetters = new()
    {
        [typeof(ServerChatEventArgs)] = GetPropertySetter<ServerChatEventArgs>(
                                            nameof(ServerChatEventArgs.Text)),
        [typeof(CommandEventArgs)] = GetPropertySetter<CommandEventArgs>(nameof(CommandEventArgs.Command))
    };
    #region GetPropertySetter

    private static MethodInfo GetPropertySetter<T>(string PropertyName) =>
        typeof(T).GetProperty(PropertyName)!.GetSetMethod(nonPublic: true)!;

    #endregion

    #endregion
    #region .Constructor

    public Plugin(Main Game) : base(Game) => _Instance = this;

    #endregion

    #region RegisterHooks

    internal static void RegisterHooks()
    {
        ServerApi.Hooks.ServerChat.Register(Instance, OnServerChat);
        ServerApi.Hooks.ServerCommand.Register(Instance, OnServerCommand);
    }

    #endregion
    #region DeregisterHooks

    internal static void DeregisterHooks()
    {
        ServerApi.Hooks.ServerChat.Deregister(Instance, OnServerChat);
        ServerApi.Hooks.ServerCommand.Deregister(Instance, OnServerCommand);
    }

    #endregion

    #region [OnPost]Initialize

    public override void Initialize() =>
        ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
    private static void OnPostInitialize(EventArgs _) =>
        CodeManager.Manager.Initialize(FromGamePostInitialize: true);

    #endregion
    #region Dispose

    protected override void Dispose(bool Disposing)
    {
        if (Disposing)
        {
            ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
            CodeManager.Manager.Disable(FromDispose: true);
        }
        base.Dispose(Disposing);
    }

    #endregion

    #region OnServerChat

    private static void OnServerChat(ServerChatEventArgs Args) =>
        OnTextInput(Args, TShock.Players[Args.Who], Args.Text);

    #endregion
    #region OnServerCommand

    private static void OnServerCommand(CommandEventArgs Args) =>
        OnTextInput(Args, TSPlayer.Server, Args.Command);

    #endregion
    #region OnTextInput

    private static void OnTextInput(HandledEventArgs Args, TSPlayer? Sender, string? Text)
    {
        if (Args.Handled
                || (Sender is null)
                || string.IsNullOrWhiteSpace(Text)
                || !Sender.HasPermission(Permissions.USE)
                || !Sender.HasPermission(Permissions.INLINE))
            return;

        CodeManager manager = CodeManager.Manager;
        if (manager.Handle(Sender, Text, CheckPermission: false))
            Args.Handled = true;
        else if (manager.ReplaceInlineCode(Sender, Text, out string? newText, CheckPermission: false))
            PropertySetters[Args.GetType()].Invoke(Args, new object[] { newText });
    }

    #endregion
}