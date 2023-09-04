namespace CSharpScripting;

public class CodeManager
{
    #region [Static/Instance]Events

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class StaticEvents
    {
        public static event PreSetCodeManagerD? PreSet;
        public static event PostSetCodeManagerD? PostSet;
        public static event GetScriptOptionsD? GetOptions;

        internal static PreSetCodeManagerD? _PreSet => PreSet;
        internal static PostSetCodeManagerD? _PostSet => PostSet;
        internal static GetScriptOptionsD? _GetOptions => GetOptions;
    }
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class InstanceEvents
    {
        public event PreToggleD? PreToggle, PreInlineCodeToggle;
        public event PostToggleD? PostToggle, PostInlineCodeToggle;

        internal PreToggleD? _PreToggle => PreToggle;
        internal PreToggleD? _PreInlineCodeToggle => PreInlineCodeToggle;
        internal PostToggleD? _PostToggle => PostToggle;
        internal PostToggleD? _PostInlineCodeToggle => PostInlineCodeToggle;

        internal InstanceEvents() { }
    }

    #endregion



    #region Manager

    private static CodeManager _Manager = new();
    public static CodeManager Manager
    {
        get => _Manager;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (StaticEvents._PreSet is PreSetCodeManagerD setManager)
            {
                value = setManager(value);
                if (value is null)
                    throw new NullReferenceException($"{nameof(StaticEvents.PreSet)} returned null.");
            }

            _Manager = value;
            bool isInitialized = _Manager.Initialize(FromGamePostInitialize: false);
            StaticEvents._PostSet?.Invoke(value, isInitialized);
        }
    }

    #endregion
    #region DefaultOptions

    private static ScriptOptions? _DefaultOptions;
    public static ScriptOptions DefaultOptions
    {
        get
        {
            if (_DefaultOptions is null)
            {
                GetBaseScriptOptions(out ScriptOptions op);
                ApplyScriptOptionsReferences(ref op);
                ApplyScriptOptionsImports(ref op);
                _DefaultOptions = op;
            }
            return GetFinalOptions();

            #region GetBaseScriptOptions

            static void GetBaseScriptOptions(out ScriptOptions Options) =>
                Options = ScriptOptions.Default
                                       .WithLanguageVersion(LanguageVersion.Latest)
                                       .WithAllowUnsafe(false);

            #endregion
            #region ApplyScriptOptionsReferences

            static void ApplyScriptOptionsReferences(ref ScriptOptions Options)
            {
                GetNewAssemblyFileNames(ref Options, out IEnumerable<string> newAssemblyFileNames);
                Options = Options.AddReferences(
                    GetNewAssemblies(AppDomain.CurrentDomain.BaseDirectory, newAssemblyFileNames));

                #region GetNewAssemblyFileNames

                static void GetNewAssemblyFileNames(ref ScriptOptions Options, out IEnumerable<string> FileNames)
                {
                    Assembly[] neededAssemblies = AppDomain.CurrentDomain
                                                           .GetAssemblies()
                                                           .Where(a => !a.IsDynamic)
                                                           .ToArray();
                    Options = Options.WithReferences(neededAssemblies.Where(a =>
                                                        !string.IsNullOrWhiteSpace(a.Location)));
                    FileNames = neededAssemblies.Where(a => string.IsNullOrWhiteSpace(a.Location))
                                                .Select(a => ((a.GetName().Name is string name)
                                                                  ? $"{name}.dll"
                                                                  : null))
                                                .Where(a => (a is not null))!;
                }

                #endregion
                #region GetNewAssemblies

                static IEnumerable<MetadataReference> GetNewAssemblies(
                    string CurrentDirectory, IEnumerable<string> FileNames)
                {
                    GetAdditionalPluginsPaths(out IList<string> additionalPluginsPaths);
                    foreach (string name in FileNames)
                    {
                        List<string> files = new(additionalPluginsPaths.Count + 3)
                        {
                            Path.Combine(CurrentDirectory, name),
                            Path.Combine(CurrentDirectory, "bin", name),
                            Path.Combine(CurrentDirectory, "ServerPlugins", name)
                        };
                        files.AddRange(additionalPluginsPaths.Select(p => Path.Combine(p, name)));
                        if (files.FirstOrDefault(File.Exists) is not string file)
                            continue;

                        MetadataReference reference;
                        try
                        {
                            using FileStream fs = File.OpenRead(file);
                            reference = MetadataReference.CreateFromStream(fs);
                        } catch { continue; }
                        yield return reference;
                    }

                    #region GetAdditionalPluginsPaths

                    // Somehow there are 2 System.Collections.Immutable references
                    // or idk what's happening here so i use reflection
                    // because otherwise it throws missing method exception...
                    static void GetAdditionalPluginsPaths(out IList<string> AdditionalPluginsPaths) =>
                        AdditionalPluginsPaths =
                            (((typeof(ServerApi).GetProperty(nameof(ServerApi.AdditionalPluginsPaths))?
                                                .GetMethod is MethodInfo getter)
                                  ? (getter.Invoke(null, null) as IList<string>)
                                  : null) ?? Array.Empty<string>());

                    #endregion
                }

                #endregion
            }

            #endregion
            #region ApplyScriptOptionsImports

            static void ApplyScriptOptionsImports(ref ScriptOptions Options)
            {
                Options = Options.AddImports(GetNamespace(nameof(System)),
                                             GetNamespace(nameof(System.Collections)),
                                             GetNamespace(nameof(System.Collections.Concurrent)),
                                             GetNamespace(nameof(System.Collections.Generic)),
                                             GetNamespace(nameof(System.Collections.ObjectModel)),
                                             GetNamespace(nameof(System.Diagnostics.CodeAnalysis)),
                                             GetNamespace(nameof(System.IO)),
                                             GetNamespace(nameof(System.IO.Compression)),
                                             GetNamespace(nameof(System.Linq)),
                                             GetNamespace(nameof(System.Reflection)),
                                             GetNamespace(nameof(System.Text)),
                                             GetNamespace(nameof(System.Text.RegularExpressions)),
                                             GetNamespace(nameof(Terraria)),
                                             GetNamespace(nameof(Terraria.DataStructures)),
                                             GetNamespace(nameof(Terraria.GameContent.Tile_Entities)),
                                             GetNamespace(nameof(Terraria.ID)),
                                             GetNamespace(nameof(TShockAPI)),
                                             GetNamespace(nameof(CSharpScripting)));

                [SuppressMessage("ReSharper", "UnusedParameter.Local")]
                static string GetNamespace(
                    string _, [CallerArgumentExpression(nameof(_))]string Name = "") => Name[7..^1];
            }

            #endregion
            #region GetFinalOptions

            static ScriptOptions GetFinalOptions() =>
                ((StaticEvents._GetOptions is GetScriptOptionsD getOptions)
                        && (getOptions(_DefaultOptions!) is ScriptOptions newOp))
                    ? newOp
                    : _DefaultOptions!;

            #endregion
        }
    }

    #endregion

    public bool IsInitialized { get; private set; }
    protected virtual bool InitializeOnGamePostInitialize => true;
    public bool IsEnabled { get; private set; }
    protected bool IsOnlyInlineCodeEnabled { get; private set; }
    public bool IsInlineCodeEnabled => (IsEnabled && IsOnlyInlineCodeEnabled);
    internal readonly ConcurrentDictionary<string, Assembly> AssemblyResolve = new();

    public CodePrefixesCollection Prefixes { get; } = new();
    public CSEnvironments Environments { get; } = new();
    public virtual Color CodeColor => Color.HotPink;
    public InstanceEvents Events { get; } = new();
    #region .Constructor

    protected internal CodeManager() { }

    #endregion



    #region Initialize

    [PublicAPI]
    public bool Initialize() => Initialize(FromGamePostInitialize: false);
    internal bool Initialize(bool FromGamePostInitialize)
    {
        if (IsInitialized || (FromGamePostInitialize && !InitializeOnGamePostInitialize))
            return false;

        try
        {
            InitializeInner();
            IsInitialized = true;
            return Enable(FromInitialize: true);
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError(ex.ToString());
            return false;
        }
    }

    #endregion
    #region InitializeInner

    protected virtual void InitializeInner()
    {
        (ScriptOptions options, Globals globals) = Environments[TSPlayer.Server];
        CSharpScript.RunAsync($"{nameof(Globals.cw)}(\"Code manager initialized.\")", options, globals)
                    .Wait();
    }

    #endregion

    #region Enable

    [PublicAPI]
    public bool Enable() => Enable(FromInitialize: false);
    private bool Enable(bool FromInitialize, bool InvokeHook = true)
    {
        if (IsEnabled)
            return false;

        bool enableInnerSuccess = false;
        try
        {
            if (!EnableInner(FromInitialize))
                return false;
            IsEnabled = enableInnerSuccess = true;

            if (!InvokeHook || (Events._PreToggle is not PreToggleD onToggle))
                return true;

            bool toggle = (onToggle.Invoke(this, Enable: true, FromInitialize) || FromInitialize);
            if (!toggle)
                Disable(FromDispose: false, InvokeHook: false);
            Events._PostToggle?.Invoke(this, Success: toggle, Enable: true, FromInitialize);
            return toggle;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError(ex.ToString());
            return enableInnerSuccess;
        }
    }

    #endregion
    #region EnableInner

    protected virtual bool EnableInner(bool FromInitialize)
    {
        RegisterPlugin();
        EnableInlineCode(FromInitialize);
        return true;
    }

    #endregion
    #region Disable[Inner]

    [PublicAPI]
    public bool Disable() => Disable(FromDispose: false);
    internal bool Disable(bool FromDispose, bool InvokeHook = true)
    {
        if (!IsEnabled)
            return false;

        bool disableInnerSuccess = false;
        try
        {
            bool disabledInner = DisableInner(FromDispose);
            if (!disabledInner && !FromDispose)
                return false;
            IsEnabled = false;
            disableInnerSuccess = true;

            if (!disabledInner && FromDispose)
                _Disable(FromDispose: true);

            if (!InvokeHook || (Events._PreToggle is not PreToggleD onToggle))
                return true;

            bool toggle = (onToggle.Invoke(this, Enable: false, FromDispose) || FromDispose);
            if (!toggle)
                Enable(FromInitialize: false, InvokeHook: false);
            Events._PostToggle?.Invoke(this, Success: toggle, Enable: false, FromDispose);
            return toggle;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError(ex.ToString());
            return disableInnerSuccess;
        }
    }
    protected virtual bool DisableInner(bool FromDispose) => _Disable(FromDispose);
    private bool _Disable(bool FromDispose)
    {
        DeregisterPlugin();
        DisableInlineCode(FromDispose);
        return true;
    }

    #endregion

    #region EnableInlineCode[Inner]

    [PublicAPI]
    public bool EnableInlineCode() => EnableInlineCode(FromInitialize: false);
    private bool EnableInlineCode(bool FromInitialize, bool InvokeHook = true)
    {
        if (IsOnlyInlineCodeEnabled)
            return false;

        bool enableInnerSuccess = false;
        try
        {
            if (!EnableInlineCodeInner(FromInitialize))
                return false;
            IsOnlyInlineCodeEnabled = enableInnerSuccess = true;

            if (!InvokeHook || (Events._PreInlineCodeToggle is not PreToggleD onToggle))
                return true;

            bool toggle = (onToggle.Invoke(this, Enable: true, FromInitialize) || FromInitialize);
            if (!toggle)
                Disable(FromDispose: false, InvokeHook: false);
            Events._PostInlineCodeToggle?.Invoke(this, Success: toggle, Enable: true, FromInitialize);
            return toggle;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError(ex.ToString());
            return enableInnerSuccess;
        }
    }
    protected virtual bool EnableInlineCodeInner(bool FromInitialize) => true;

    #endregion
    #region DisableInlineCode[Inner]

    [PublicAPI]
    public bool DisableInlineCode() => DisableInlineCode(FromDispose: false);
    private bool DisableInlineCode(bool FromDispose, bool InvokeHook = true)
    {
        if (!IsOnlyInlineCodeEnabled)
            return false;

        bool disableInnerSuccess = false;
        try
        {
            if (!DisableInlineCodeInner(FromDispose) && !FromDispose)
                return false;
            IsOnlyInlineCodeEnabled = false;
            disableInnerSuccess = true;

            if (!InvokeHook || (Events._PreInlineCodeToggle is not PreToggleD onToggle))
                return true;

            bool toggle = (onToggle.Invoke(this, Enable: false, FromDispose) || FromDispose);
            if (!toggle)
                EnableInlineCode(FromInitialize: false, InvokeHook: false);
            Events._PostInlineCodeToggle?.Invoke(this, Success: toggle, Enable: false, FromDispose);
            return toggle;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError(ex.ToString());
            return disableInnerSuccess;
        }
    }
    protected virtual bool DisableInlineCodeInner(bool FromDispose) => true;

    #endregion

    #region RegisterPlugin

    protected bool RegisterPlugin()
    {
        try
        {
            RegisterPluginInner();
            return true;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError(ex.ToString());
            return false;
        }
    }

    #endregion
    #region RegisterPluginInner

    protected virtual bool RegisterPluginInner()
    {
        Plugin.RegisterPlugin();
        return true;
    }

    #endregion
    #region DeregisterPlugin

    protected bool DeregisterPlugin()
    {
        try
        {
            DeregisterPluginInner();
            return true;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError(ex.ToString());
            return false;
        }
    }

    #endregion
    #region DeregisterPluginInner

    protected virtual bool DeregisterPluginInner()
    {
        Plugin.DeregisterPlugin();
        return true;
    }

    #endregion



    #region HasExecutePermission

    public virtual bool HasExecutePermission([NotNullWhen(true)]TSPlayer? Sender) =>
        (Sender?.HasPermission(Permissions.USE) is true);

    #endregion
    #region HasInlinePermission

    public virtual bool HasInlinePermission([NotNullWhen(true)]TSPlayer? Sender) =>
        (HasExecutePermission(Sender) && Sender.HasPermission(Permissions.INLINE));

    #endregion

    #region ReplaceVariables[Inner]

    private const string VAR_GROUP = "var";
    private static readonly Regex VAR_REGEX = new($@"\$(?<{VAR_GROUP}>[a-zA-Z_][a-zA-Z_\d]*)");
    private string ReplaceVariables(string Code)
    {
        try { return ReplaceVariablesInner(Code); }
        catch { return Code; }
    }
    protected virtual string ReplaceVariablesInner(string Code) =>
        VAR_REGEX.Replace(Code, (m => $"{nameof(Globals.kv)}[\"{m.Groups[VAR_GROUP].Value}\"]"));

    #endregion

    #region Handle[Inner]

    [SuppressMessage("ReSharper", "MergeIntoNegatedPattern")]
    public bool Handle(TSPlayer? Sender, string? Text, bool CheckEnabled = true, bool CheckPermission = true)
    {
        if ((Sender is null)
                || string.IsNullOrWhiteSpace(Text)
                || (CheckEnabled && !IsEnabled)
                || (CheckPermission && !HasExecutePermission(Sender))
                || (Environments[Sender] is not CSEnvironment env)
                || !env.IsReady
                || !Prefixes.TryGet(Text, out string? showCode, out CodePrefix? codePrefix))
            return false;
        
        _ = HandleInner(Sender, env, codePrefix, showCode,
                        ReplaceVariables((codePrefix.AddSemicolon ? $"{showCode};" : showCode)))
            .ContinueWith(t => TShock.Log.ConsoleError(t.Exception!.ToString()),
                          TaskContinuationOptions.OnlyOnFaulted);
        return true;
    }
    protected virtual async Task HandleInner(TSPlayer Sender, CSEnvironment Environment,
                                             CodePrefix CodePrefix, string ShowCode, string HandleCode) =>
        await CodePrefix.Handle(Sender, Environment, HandleCode, ShowCode);

    #endregion

    #region ReplaceInlineCode

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public bool ReplaceInlineCode(TSPlayer? Sender, string? Text, [MaybeNullWhen(false)]out string NewText,
                                  bool CheckEnabled = true, bool CheckPermission = true)
    {
        NewText = null;
        if ((Sender is null)
                || string.IsNullOrWhiteSpace(Text)
                || (CheckEnabled && !IsInlineCodeEnabled)
                || (CheckPermission && !HasInlinePermission(Sender)))
            return false;

        string? error;
        try
        {
            if (ReplaceInlineCodeInner(Sender, Text, out NewText, out error))
                if (NewText is null)
                    error ??= $"[{nameof(CSharpScripting)}] Something went wrong...";
                else if (error is not null)
                    NewText = null;
                else
                    return true;
        } catch (Exception ex) { error = ex.ToString(); }

        Sender.SendErrorMessage(error);
        return false;
    }

    #endregion
    #region ReplaceInlineCodeInner

    protected const string WITH_CODE_RETURN_GROUP = "with_code", ONLY_RETURN_CODE_GROUP = "only_code";
    protected const string WITH_CODE_RETURN_PATTERN = $@"(?:```(?<{WITH_CODE_RETURN_GROUP}>[^`]+)```)";
    protected const string ONLY_RETURN_CODE_PATTERN = $@"(?:``(?<{ONLY_RETURN_CODE_GROUP}>[^`]+)``)";
    protected static readonly Regex INLINE_CODE_REGEX =
        new(@$"{WITH_CODE_RETURN_PATTERN}|{ONLY_RETURN_CODE_PATTERN}");
    private sealed class BreakException : Exception { }
    protected virtual bool ReplaceInlineCodeInner(TSPlayer Sender, string Text,
                                                  [MaybeNullWhen(false)]out string NewText,
                                                  [MaybeNullWhen(true)]out string Error)
    {
        string? error = NewText = Error = null;
        try
        {
            (string @using, ScriptOptions options, Globals globals) = Environments[Sender];
            NewText = INLINE_CODE_REGEX.Replace(Text, (m =>
            {
                bool withCode = m.Groups[WITH_CODE_RETURN_GROUP].Success;
                if (!GetInlineCodeReplacement((withCode
                                                    ? m.Groups[WITH_CODE_RETURN_GROUP].Value
                                                    : m.Groups[ONLY_RETURN_CODE_GROUP].Value),
                                              withCode, options, globals,
                                              out string? replacement, out error))
                    throw new BreakException();

                return replacement;
            }));
            return true;
        }
        catch (BreakException)
        {
            Error = error!;
            return false;
        }
    }

    #endregion
    #region GetInlineCodeReplacement

    protected virtual bool GetInlineCodeReplacement(string Code, bool WithCode,
                                                    ScriptOptions Options, Globals Globals,
                                                    [MaybeNullWhen(false)]out string Replacement,
                                                    [MaybeNullWhen(true)]out string Error)
    {
        Replacement = Error = null;
        string? replacement = null, error = null;
        string tagStart = $"[c/{CodeColor.Hex3()}:";
        const string TAG_END = "]";
        try
        {
            CSharpScript.RunAsync($"return {ReplaceVariables(Code)};", Options, Globals)
                        .ContinueWith(t => (replacement, error) =
                                                (ToStringNull(t.Result.ReturnValue),
                                                 (t.Exception ?? t.Result.Exception)?.ToString()))
                        .Wait();
            if (error is not null)
            {
                Error = error;
                return false;
            }

            string withCode =
                (WithCode
                     ? $"{Code.Replace(TAG_END, $"{TAG_END}{tagStart}{TAG_END}{TAG_END}{tagStart}")}: "
                     : string.Empty);
            Replacement = $"{tagStart}❮{withCode}{replacement}❯{TAG_END}";
            return true;
        }
        catch (Exception ex)
        {
            Error = (error ?? ex.ToString());
            return false;
        }
    }

    #endregion



    #region ToString

    public override string ToString() => $"{GetType().Name} [" +
                                         $"Initialized: {IsInitialized}; " +
                                         $"Enabled: {IsEnabled}; " +
                                         $"Inline code enabled: {IsOnlyInlineCodeEnabled}; " +
                                         $"Prefixes: {Prefixes}; " +
                                         $"Code color: {CodeColor}]";

    #endregion
}