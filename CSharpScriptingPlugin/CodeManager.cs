#region Using

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using TShockAPI;

#endregion
namespace CSharpScriptingPlugin;

public class CodeManager
{
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
    #region Prefixes

    protected ReadOnlyCollection<string> DefaultPrefixes { get; private set; } =
        new(new[] { DEFAULT_SHOW_PREFIX, DEFAULT_EXECUTE_PREFIX });
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
        DefaultPrefixes = new(GetPrefixes(new[] { _ExecutePrefix, _ShowPrefix }).ToArray());
    private static IEnumerable<string> GetPrefixes(IEnumerable<string> Prefixes) =>
        Prefixes.Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .Select(p => p.ToLower());
    
    #endregion
    #region Options
    
    private static ScriptOptions? _DefaultOptions;
    protected static ScriptOptions DefaultOptions
    {
        get
        {
            if (_DefaultOptions is null)
            {
                Debugger.Launch();
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                _DefaultOptions =
                    ScriptOptions.Default
                                 .WithLanguageVersion(LanguageVersion.Latest)
                                 .WithMetadataResolver(
                                     ScriptMetadataResolver.Default
                                                           .WithBaseDirectory(dir)
                                                           .WithSearchPaths(
                                                               Path.Combine(dir, "bin"),
                                                               Path.Combine(dir, "ServerPlugins")));
                                 //.WithReferences(typeof(System.Int32).Assembly,
                                 //                typeof(System.Collections.BitArray).Assembly,
                                 //                typeof(TerrariaApi.Server.ApiVersionAttribute).Assembly,
                                 //                typeof(Terraria.Main).Assembly,
                                 //                typeof(System.Linq.Enumerable).Assembly,
                                 //                typeof(System.Collections.Immutable.ImmutableList).Assembly,
                                 //                typeof(System.Timers.Timer).Assembly,
                                 //                typeof(System.Console).Assembly,
                                 //                typeof(System.Collections.Concurrent.ConcurrentBag<System.Int32>).Assembly,
                                 //                typeof(Newtonsoft.Json.JsonConvert).Assembly,
                                 //                typeof(System.Net.Sockets.TcpClient).Assembly,
                                 //                typeof(On.Terraria.Main).Assembly,
                                 //                typeof(System.Diagnostics.Process).Assembly,
                                 //                typeof(System.Collections.CaseInsensitiveComparer).Assembly,
                                 //                typeof(System.Diagnostics.FileVersionInfo).Assembly,
                                 //                typeof(System.Buffers.Text.Base64).Assembly,
                                 //                typeof(System.Text.RegularExpressions.Regex).Assembly,
                                 //                typeof(System.Uri).Assembly,
                                 //                typeof(System.Collections.ObjectModel.ObservableCollection<System.Int32>).Assembly,
                                 //                typeof(System.Data.IDbConnection).Assembly,
                                 //                typeof(CSharpScriptingPlugin.Configuration.Globals).Assembly,
                                 //                typeof(System.Collections.Immutable.ImmutableArray).Assembly,
                                 //                typeof(Microsoft.CodeAnalysis.Scripting.ScriptOptions).Assembly,
                                 //                typeof(Microsoft.Data.Sqlite.SqliteConnection).Assembly,
                                 //                typeof(MySql.Data.MySqlClient.MySqlConnection).Assembly,
                                 //                typeof(System.Net.Http.HttpClient).Assembly);
                HashSet<string> errors = new();
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    ScriptOptions options;
                    try
                    {
                        Type t = assembly.GetType();
                        Console.WriteLine(t);
                        options = _DefaultOptions.AddReferences(assembly);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(assembly.FullName);
                        Type[] types = assembly.GetTypes().Where(t => t.IsPublic).ToArray();
                        if (types.Any())
                            Console.WriteLine(types);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(assembly.FullName);
                        string msg = ex.ToString();
                        if (errors.Add(msg))
                            Console.WriteLine(msg);
                        Type[] types = assembly.GetTypes().Where(t => t.IsPublic).ToArray();
                        if (types.Any())
                            Console.WriteLine(types);
                        continue;
                    }
                    _DefaultOptions = options;
                }
                Console.ResetColor();
                _DefaultOptions = _DefaultOptions.AddImports("System",
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
                                                             "Terraria.ID");
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
        Globals globals = (GetGlobalsInner(Sender) ?? new());
        return (globals with { me = globals._me ?? Sender });
    }
    [return: MaybeNull]
    protected virtual Globals GetGlobalsInner(TSPlayer Sender) => new();

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
        else if ((Input[^1] != ';') && (Input[^1] != '}'))
            Input = $"{Input};";

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
            return await HandleInner(Sender, Prefix, Code, Options, GetGlobals(Sender));
        }
        catch (Exception exception)
        {
            Sender.SendErrorMessage(exception.ToString());
        }
        return true;
    }

    #endregion
    #region HandleInner
    
    protected virtual async Task<bool> HandleInner(TSPlayer Sender, string Prefix, string Code,
                                                   ScriptOptions Options, Globals Globals)
    {
        if (Prefix == ExecutePrefix)
        {
            await CSharpScript.RunAsync(Code, Options, Globals);
            return true;
        }
        else if (Prefix == ShowPrefix)
        {
            await CSharpScript.RunAsync($"return {Code}", Options, Globals)
                              .ContinueWith(s => Globals.cw(s.Result.ReturnValue));
            return true;
        }
        return false;
    }

    #endregion
}