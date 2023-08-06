#region Using

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TShockAPI;

#endregion
namespace CSharpScriptingPlugin;

public class CodeManager
{
    public const string PLAYER_DATA_KEY = $"{nameof(CSharpScriptingPlugin)}_Data";
    #region Manager
    
    private static CodeManager? _Manager = new();
    public static CodeManager? Manager
    {
        get => _Manager;
        set
        {
            CodeManager? val = value;
            if (OnSetManager is SetManagerD setManager)
                val = setManager(val);
            _Manager = val;
            _Manager?.Initialize();
        }
    }

    #endregion
    #region OnSetManager

    public static event SetManagerD? OnSetManager;

    #endregion

    #region IsInitialized

    public bool IsInitialized { get; private set; }

    #endregion
    #region ExecutePrefix

    protected const string DEFAULT_EXECUTE_PREFIX = ";";
    private string _ExecutePrefix = DEFAULT_EXECUTE_PREFIX;
    [AllowNull]
    public string ExecutePrefix
    {
        get { lock (DefaultOptions) return _ExecutePrefix; }
        set
        {
            lock (DefaultOptions)
            {
                _ExecutePrefix = (value ?? DEFAULT_EXECUTE_PREFIX);
                UpdateDefaultPrefixes();
            }
        }
    }

    #endregion
    #region ShowPrefix

    protected const string DEFAULT_SHOW_PREFIX = ";;";
    private string _ShowPrefix = DEFAULT_SHOW_PREFIX;
    [AllowNull]
    public string ShowPrefix
    {
        get { lock (DefaultOptions) return _ShowPrefix; }
        set
        {
            lock (DefaultOptions)
            {
                _ShowPrefix = (value ?? DEFAULT_SHOW_PREFIX);
                UpdateDefaultPrefixes();
            }
        }
    }

    #endregion
    #region SignaturePrefix

    protected const string DEFAULT_SIGNATURE_PREFIX = ";=";
    private string _SignaturePrefix = DEFAULT_SIGNATURE_PREFIX;
    [AllowNull]
    public string SignaturePrefix
    {
        get { lock (DefaultOptions) return _SignaturePrefix; }
        set
        {
            lock (DefaultOptions)
            {
                _SignaturePrefix = (value ?? DEFAULT_SIGNATURE_PREFIX);
                UpdateDefaultPrefixes();
            }
        }
    }

    #endregion
    #region Prefixes

    protected ReadOnlyCollection<string> DefaultPrefixes { get; private set; } =
        new(new[] { DEFAULT_SHOW_PREFIX, DEFAULT_SIGNATURE_PREFIX, DEFAULT_EXECUTE_PREFIX });
    protected virtual IEnumerable<string> PrefixesInner => DefaultPrefixes;
    protected IEnumerable<string> Prefixes
    {
        get
        {
            IEnumerable<string> prefixes = PrefixesInner;
            return (ReferenceEquals(prefixes, DefaultPrefixes)
                        ? prefixes
                        : GetPrefixes(prefixes));
        }
    }
    
    private void UpdateDefaultPrefixes() =>
        DefaultPrefixes = new(GetPrefixes(new[] { _ExecutePrefix, _ShowPrefix, _SignaturePrefix }).ToArray());
    private static IEnumerable<string> GetPrefixes(IEnumerable<string> Prefixes) =>
        Prefixes.Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .Select(p => p.ToLower())
                .OrderByDescending(p => p.Length);

    #endregion
    #region Options

    private static ScriptOptions? _DefaultOptions;
    protected static ScriptOptions DefaultOptions
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
                                             "CSharpScriptingPlugin");
            }
            return _DefaultOptions;
        }
    }
    private ScriptOptions _Options = DefaultOptions;
    [AllowNull]
    public ScriptOptions Options
    {
        get => _Options;
        set => _Options = (value ?? DefaultOptions);
    }

    #endregion

    #region Initialize

    public bool Initialize()
    {
        if (IsInitialized)
            return false;

        InitializeInner(Options, GetGlobals(TSPlayer.Server));
        return (IsInitialized = true);
    }
    protected virtual void InitializeInner(ScriptOptions Options, Globals Globals) =>
        CSharpScript.RunAsync($"{nameof(Globals.cw)}(\"Code manager initialized.\")", Options, Globals)
                    .Wait();

    #endregion
    #region GetGlobals[Inner]
    // ReSharper disable UseNullableAnnotationInsteadOfAttribute

    public Globals GetGlobals(TSPlayer Sender)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        return (GetGlobalsInner(Sender) ?? _GetGlobals(Sender));
    }
    [return: MaybeNull]
    protected virtual Globals GetGlobalsInner(TSPlayer Sender) => _GetGlobals(Sender);
    private static Globals _GetGlobals(TSPlayer Sender)
    {
        if (!Sender.ContainsData(PLAYER_DATA_KEY))
            Sender.SetData(PLAYER_DATA_KEY, new Globals(Sender));
        return Sender.GetData<Globals>(PLAYER_DATA_KEY);
    }

    // ReSharper restore UseNullableAnnotationInsteadOfAttribute
    #endregion

    #region ShouldHandle

    public bool ShouldHandle(TSPlayer Sender, string Input,
                             [MaybeNullWhen(false)]out string Prefix,
                             [MaybeNullWhen(false)]out string Code)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        ArgumentNullException.ThrowIfNull(Input);
        Prefix = Code = null;
        Input = Input.Trim();
        if (string.IsNullOrWhiteSpace(Input))
            return false;

        string lowerInput = Input.ToLower();
        Prefix = Prefixes.FirstOrDefault(p => lowerInput.StartsWith(p));
        if (Prefix is not null)
        {
            Code = Input[Prefix.Length..];
            return true;
        }
        return false;
    }

    #endregion
    #region Handle

    public async Task<bool> Handle(TSPlayer Sender, string Prefix, string Code)
    {
        ArgumentNullException.ThrowIfNull(Sender);
        ArgumentNullException.ThrowIfNull(Prefix);
        ArgumentNullException.ThrowIfNull(Code);
        try
        {
            await ShowInput(Sender, Prefix, Code);
            return await HandleInner(Sender, Prefix, Code, Options, GetGlobals(Sender));
        }
        catch (Exception exception)
        {
            Sender.SendErrorMessage(exception.ToString());
        }
        return true;
    }

    #endregion
    #region ShowInput

    protected virtual Task ShowInput(TSPlayer Sender, string Prefix, string Code)
    {
        Globals.admins.ForEach(p => p.SendMessage($"{Prefix}{Code}", Color.HotPink));
        return Task.CompletedTask;
    }

    #endregion
    #region HandleInner

    protected virtual async Task<bool> HandleInner(TSPlayer Sender, string Prefix, string Code,
                                                   ScriptOptions Options, Globals Globals)
    {
        string code = $"{Code};";
        if (Prefix == ExecutePrefix)
        {
            await CSharpScript.RunAsync(code, Options, Globals);
            return true;
        }
        else if (Prefix == ShowPrefix)
        {
            await CSharpScript.RunAsync($"return {code}", Options, Globals)
                              .ContinueWith(s => Globals.cw(s.Result.ReturnValue));
            return true;
        }
        else if (Prefix == SignaturePrefix)
        {
            await CSharpScript.RunAsync($"return {code}", Options, Globals)
                              .ContinueWith(s => s.Result
                                                  .ReturnValue
                                                  .GetType()
                                                  .GetMembers()
                                                  .ForEach(m => Globals.cw(m)));
        }
        return false;
    }

    #endregion
}