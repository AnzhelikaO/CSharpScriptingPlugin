#region Using

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using CSharpScripting.Configuration.Delegates;
using CSharpScripting.Configuration.Prefixes;
using CSharpScripting.Configuration.PlayerManagers;
using System.Text.RegularExpressions;
using Permissions = CSharpScripting.Configuration.Permissions;

// ReSharper disable UseNullableAnnotationInsteadOfAttribute

#endregion
namespace CSharpScripting;

public class CodeManager
{
    #region Manager
    
    private static CodeManager _Manager = new();
    [AllowNull]
    public static CodeManager Manager
    {
        get => _Manager;
        set
        {
            CodeManager? val = value;
            if (OnSetManager is SetManagerD setManager)
                val = setManager(val);
            if ((val is null) || ReferenceEquals(val, _Manager))
                return;

            _Manager = val;
            _Manager.Initialize();
        }
    }

    #endregion
    #region OnSetManager

    public static event SetManagerD? OnSetManager;

    #endregion

    #region IsInitialized

    public bool IsInitialized { get; private set; }

    #endregion
    #region Prefixes, Options, Globals

    public CodePrefixesCollection Prefixes { get; } = new();
    public ScriptOptionsPlayerManager Options { get; } = new();
    public GlobalsPlayerManager Globals { get; } = new();

    #endregion
    #region DefaultOptions

    private ScriptOptions? _DefaultOptions;
    public ScriptOptions DefaultOptions
    {
        get
        {
            if (_DefaultOptions is null)
            {
                Assembly[] neededAssemblies = AppDomain.CurrentDomain
                                                       .GetAssemblies()
                                                       .Where(a => !a.IsDynamic)
                                                       .ToArray();
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                _DefaultOptions =
                    ScriptOptions.Default
                                 .WithLanguageVersion(LanguageVersion.Latest)
                                 .WithAllowUnsafe(false)
                                 .AddReferences(neededAssemblies
                                                    .Where(a => !string.IsNullOrWhiteSpace(a.Location)))
                                 .AddReferences(neededAssemblies
                                                .Where(a => string.IsNullOrWhiteSpace(a.Location))
                                                .Select(a =>
                                                {
                                                    if (!string.IsNullOrWhiteSpace(a.Location)
                                                     || (a.GetName().Name is not string name))
                                                        return null;

                                                    name = $"{name}.dll";
                                                    if (new[]
                                                        {
                                                            Path.Combine(dir, name),
                                                            Path.Combine(dir, "bin", name),
                                                            Path.Combine(dir, "ServerPlugins", name)
                                                        }.FirstOrDefault(f => File.Exists(f)) is not string
                                                        file)
                                                        return null;
                                                    using FileStream fs = File.OpenRead(file);
                                                    return MetadataReference.CreateFromStream(fs);
                                                })
                                                .Where(r => (r is not null)))
                                 .AddImports("System",
                                             "System.Collections",
                                             "System.Collections.Concurrent",
                                             "System.Collections.Generic",
                                             "System.Collections.ObjectModel",
                                             "System.Diagnostics.CodeAnalysis",
                                             "System.IO",
                                             "System.IO.Compression",
                                             "System.Linq",
                                             "System.Reflection",
                                             "System.Text",
                                             "System.Text.RegularExpressions",
                                             "Terraria",
                                             "Terraria.DataStructures",
                                             "Terraria.GameContent.Tile_Entities",
                                             "Terraria.ID",
                                             "TShockAPI",
                                             "CSharpScripting");
            }
            return _DefaultOptions;
        }
    }

    #endregion

    #region Initialize[Inner]

    public bool Initialize()
    {
        if (IsInitialized)
            return false;

        try
        {
            InitializeInner();
            return (IsInitialized = true);
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError(ex.ToString());
            return false;
        }
    }
    protected virtual void InitializeInner() =>
        CSharpScript.RunAsync($"{nameof(Configuration.Globals.cw)}(\"Code manager initialized.\")",
                              Options.Get(TSPlayer.Server), Globals.Get(TSPlayer.Server))
                    .Wait();

    #endregion

    #region Handle[Inner]

    protected internal bool Handle(TSPlayer? Sender, string Text, bool Force = false)
    {
        if ((Sender is null)
                || string.IsNullOrWhiteSpace(Text)
                || !Sender.HasPermission(Permissions.USE)
                || !Prefixes.TryGet(Text, out string? showCode, out CodePrefix? codePrefix))
            return false;

        string handleCode = $"{ReplaceCode(showCode)}{(codePrefix.AddSemicolon ? ";" : string.Empty)}";
        _ = HandleInner(Sender, showCode, handleCode, codePrefix)
            .ContinueWith(t => TShock.Log.ConsoleError(t.Exception?.ToString()),
                          TaskContinuationOptions.OnlyOnFaulted);
        return true;
    }
    protected virtual async Task HandleInner(TSPlayer Sender, string ShowCode,
                                             string HandleCode, CodePrefix CodePrefix) =>
        await CodePrefix.Handle(Sender, ShowCode, HandleCode);

    #endregion
    #region ReplaceCode

    private const string VAR = "var";
    private static readonly Regex VAR_REGEX = new($@"\$(?<{VAR}>[a-zA-Z_][a-zA-Z\d_]*)");
    protected internal virtual string ReplaceCode(string Text) =>
        VAR_REGEX.Replace(Text, (m => $"{nameof(Configuration.Globals.kv)}[\"{m.Groups[VAR].Value}\"]"));

    #endregion
}