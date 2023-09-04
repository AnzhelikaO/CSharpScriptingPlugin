namespace CSharpScripting.Environments;

#warning TODO: environment hooks
public sealed class CSEnvironment
{
    #region Files

    public static class Files
    {
        public const string ROOT_DIRECTORY_NAME = nameof(CSharpScripting);
        public const string USING_FILE_NAME = "Using.cs";
        public const string GLOBALS_CONSTRUCTOR_FILE_NAME = $"{nameof(Globals)}Constructor.cs";
        public const string GLOBALS_MEMBERS_FILE_NAME = $"{nameof(Globals)}Members.cs";
    }

    #endregion
    #region UserInfo

    private sealed record UserInfo(
        int UserID, string UsingFile, string GlobalsConstructorFile, string GlobalsMembersFile)
    {
        public static bool Get(TSPlayer Player, [MaybeNullWhen(false)]out UserInfo Info)
        {
            Info = null;
            if (Player.Account?.ID is not int userID)
                return false;

            string root = Path.Combine(Environment.CurrentDirectory,
                                       Files.ROOT_DIRECTORY_NAME,
                                       userID.ToString());
            Info = new(userID,
                       UsingFile: Path.Combine(root, Files.USING_FILE_NAME),
                       GlobalsConstructorFile: Path.Combine(root, Files.GLOBALS_CONSTRUCTOR_FILE_NAME),
                       GlobalsMembersFile: Path.Combine(root, Files.GLOBALS_MEMBERS_FILE_NAME));
            return true;
        }
    }

    #endregion

    public TSPlayer Player { get; }
    private readonly HashSet<string> UsingNoLock;
    [PublicAPI]public IEnumerable<string> UsingList => GetInLock(() => UsingNoLock.ToList());
    private string UsingStringNoLock => string.Join('\n', UsingNoLock);
    [PublicAPI]public string Using => GetInLock(() => UsingStringNoLock);
    #region Options

    private ScriptOptions OptionsNoLock;
    public ScriptOptions Options
    {
        get => GetInLock(() => OptionsNoLock);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetInLock(() => OptionsNoLock = value);
        }
    }

    #endregion
    #region Globals

    internal Globals GlobalsNoLock { get; private set; }
    public Globals Globals
    {
        get => GetInLock(() => GlobalsNoLock);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetInLock(() => GlobalsNoLock = value);
        }
    }

    #endregion
    public bool IsReady { get; private set; }
    public int ReloadIndex { get; private set; }
    private readonly SemaphoreSlim Semaphore;
    internal readonly long Token;
    #region .Constructor

    public CSEnvironment(TSPlayer Player, bool LoadUsing = true, bool LoadGlobals = true)
    {
        ArgumentNullException.ThrowIfNull(Player);

        this.Player = Player;
        UsingNoLock = new(25);
        OptionsNoLock = CodeManager.DefaultOptions;
        GlobalsNoLock = new(Player);
        IsReady = false;
        ReloadIndex = 0;
        Semaphore = new(1, 1);
        Token = Tokens.Take(Player);
        _ = Reload(LoadUsing, LoadGlobals, AnnounceFinish: false);
    }

    #endregion

    #region Reload

    public async Task<bool> Reload(bool Using = true, bool Globals = true, bool AnnounceFinish = true)
    {
        if (!CheckReady())
            return false;
        else if (!Using && !Globals)
            return true;

        bool ok;
        try
        {
            await Task.Delay(1);
            await Semaphore.WaitAsync();
            IsReady = false;
            
            if (!UserInfo.Get(Player, out UserInfo? info))
            {
                Finish();
                return true;
            }
            
            ReloadIndex++;
            if (Using)
                await ReloadUsing();
            if (Globals)
                await ReloadGlobals();
            ok = true;
            
            #region ReloadUsing

            async Task ReloadUsing()
            {
                if (await GetUsing() is not string[] @using)
                    return;

                foreach (string u in @using)
                    UsingNoLock.Add(u);
                if (AnnounceFinish)
                    Player.SendMessage($"[{nameof(CSharpScripting)}] Reloaded {Files.USING_FILE_NAME}",
                                       CodeManager.Manager.CodeColor);

                #region GetUsing

                async Task<string[]?> GetUsing()
                {
                    if (!File.Exists(info.UsingFile))
                        return null;

                    string[] @using = (await File.ReadAllLinesAsync(info.UsingFile))
                                      .Select(l => l.Trim())
                                      .Where(l => (l.StartsWith("using ") && (l.Length > 6)))
                                      .Select(UnifyUsing)
                                      .ToArray();
                    if (!@using.Any())
                        return null;
                    
                    try
                    {
                        await CSharpScript.RunAsync(string.Join('\n', @using), OptionsNoLock, GlobalsNoLock);
                        return @using;
                    }
                    catch (Exception ex)
                    {
                        Player.SendErrorMessage(
                            $"[{nameof(CSharpScripting)}] Invalid {Files.USING_FILE_NAME}: {ex}");
                        return null;
                    }
                }

                #endregion
            }

            #endregion
            #region ReloadGlobals
            
            async Task ReloadGlobals()
            {
                string name = $"AdminGlobals_{info.UserID}_{ReloadIndex}";
                (bool newConstructor, string ctorCode) = await GetConstructorCode();
                (bool newMembers, string membersCode) = await GetMembersCode();
                if (!newConstructor && !newMembers)
                    return;
                
                try
                {
                    await SetNewGlobals();
                    ImportAssembly();
                    AnnounceOK();
                } catch (Exception ex) { AnnounceException(ex); }

                #region GetConstructorCode

                async Task<(bool NewConstructor, string Code)> GetConstructorCode()
                {
                    if (!File.Exists(info.GlobalsConstructorFile))
                        return default;

                    string code = await File.ReadAllTextAsync(info.GlobalsConstructorFile);
                    const string COPY_FROM_PREVIOUS = Configuration.Globals.COPY_FROM_PREVIOUS;
                    return (string.IsNullOrWhiteSpace(code)
                                ? default
                                : (NewConstructor: true,
                                   Code: $$"""
                                           public {{name}}()
                                           {
                                               {{COPY_FROM_PREVIOUS}}({{Token}});
                                               {{code}};
                                           }
                                           """));
                }

                #endregion
                #region GetMembersCode

                async Task<(bool NewMembers, string Code)> GetMembersCode()
                {
                    if (!File.Exists(info.GlobalsMembersFile))
                        return default;

                    string code = await File.ReadAllTextAsync(info.GlobalsMembersFile);
                    return (string.IsNullOrWhiteSpace(code)
                                ? default
                                : (NewMembers: true, Code: code));
                }

                #endregion
                #region SetNewGlobals

                async Task SetNewGlobals() =>
                    GlobalsNoLock = ((await CSharpScript.RunAsync($$"""
                        {{UsingStringNoLock}}
                        using {{nameof(CSharpScripting)}}.{{nameof(Configuration)}};

                        public class {{name}} : {{nameof(Configuration.Globals)}}
                        {
                            {{ctorCode}}
                            {{membersCode}}
                        }

                        return new {{name}}();
                        """, OptionsNoLock, GlobalsNoLock)).ReturnValue as Globals)!;

                #endregion
                #region ImportAssembly

                void ImportAssembly()
                {
                    Assembly assembly = GlobalsNoLock.GetType().Assembly;
                    CodeManager.Manager.AssemblyResolve[assembly.FullName!] = assembly;
                    GlobalsNoLock.FromScriptReference = MetadataReference.CreateFromImage(
                        new AssemblyGenerator().GenerateAssemblyBytes(assembly));
                    OptionsNoLock = OptionsNoLock.AddReferences(GlobalsNoLock.FromScriptReference);
                }

                #endregion
                #region AnnounceOK

                void AnnounceOK()
                {
                    if (!AnnounceFinish)
                        return;

                    string reloaded =
                        ((newConstructor && newMembers)
                             ? $"{Files.GLOBALS_CONSTRUCTOR_FILE_NAME} and {Files.GLOBALS_MEMBERS_FILE_NAME}"
                             : (newConstructor
                                    ? Files.GLOBALS_CONSTRUCTOR_FILE_NAME
                                    : Files.GLOBALS_MEMBERS_FILE_NAME));
                    Player.SendMessage($"[{nameof(CSharpScripting)}] Reloaded {reloaded}",
                                       CodeManager.Manager.CodeColor);
                }

                #endregion
                #region AnnounceException

                void AnnounceException(Exception Exception)
                {
                    string invalid =
                        (newConstructor && newMembers)
                            ? $"Invalid {Files.GLOBALS_CONSTRUCTOR_FILE_NAME} " +
                              $"or {Files.GLOBALS_MEMBERS_FILE_NAME}: {Exception}"
                            : (newConstructor
                                   ? $"Invalid {Files.GLOBALS_CONSTRUCTOR_FILE_NAME}: {Exception}"
                                   : $"Invalid {Files.GLOBALS_MEMBERS_FILE_NAME}: {Exception}");
                    Player.SendErrorMessage($"[{nameof(CSharpScripting)}] {invalid}");
                }

                #endregion
            }

            #endregion
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[{nameof(CSharpScripting)}] {ex}");
            ok = false;
        }
        finally { Finish(); }
        return ok;

        #region Finish

        void Finish()
        {
            IsReady = true;
            Semaphore.Release();
        }

        #endregion
    }

    #endregion
    #region Reset

    public async Task<bool> Reset(bool Using = true, bool Options = true,
                                  bool Globals = true, bool AnnounceFinish = true)
    {
        if (!CheckReady())
            return false;

        bool ok;
        try
        {
            await Semaphore.WaitAsync();
            IsReady = false;

            List<string> resetParts = new(3);
            if (Using)
            {
                UsingNoLock.Clear();
                resetParts.Add("using statements");
            }
            if (Options)
            {
                this.Options = ((Globals || (this.Globals.FromScriptReference is not MetadataReference @ref))
                                   ? CodeManager.DefaultOptions
                                   : CodeManager.DefaultOptions.AddReferences(@ref));
                resetParts.Add("script options");
            }
            if (Globals)
            {
                this.Globals = new(this.Globals);
                resetParts.Add("globals object");
            }

            if (AnnounceFinish && resetParts.Any())
            {
                string reset = ((resetParts.Count > 2)
                                    ? $"{string.Join(", ", resetParts.Take(..^1))} and {resetParts[^1]}"
                                    : string.Join(" and ", resetParts));
                Player.SendMessage($"[{nameof(CSharpScripting)}] Reset {reset}",
                                   CodeManager.Manager.CodeColor);
            }
            ok = true;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[{nameof(CSharpScripting)}] {ex}");
            ok = false;
        }
        finally
        {
            IsReady = true;
            Semaphore.Release();
        }
        return ok;
    }

    #endregion
    #region Deconstruct

    public void Deconstruct(out ScriptOptions Options, out Globals Globals) =>
        (Options, Globals) = GetInLock(() => (OptionsNoLock, GlobalsNoLock));
    public void Deconstruct(out string Using, out ScriptOptions Options, out Globals Globals) =>
        (Using, Options, Globals) = GetInLock(() => (UsingStringNoLock, OptionsNoLock, GlobalsNoLock));

    #endregion

    #region AddUsing

    public Task<int> AddUsing(IEnumerable<string> Using) => _AddUsing(Using);
    [PublicAPI]public Task<int> AddUsing(params string[] Using) => _AddUsing(Using);
    private async Task<int> _AddUsing(IEnumerable<string?>? Using)
    {
        if (!CheckReady())
            return -1;

        int count = 0;
        await Semaphore.WaitAsync();
        if (Using is not null)
            foreach (string? @using in Using)
                if (@using is not null)
                {
                    UsingNoLock.Add(UnifyUsing(@using));
                    count++;
                }
        Semaphore.Release();
        return count;
    }

    #endregion
    #region RemoveUsing

    [PublicAPI]public async Task<int> RemoveUsing(IEnumerable<string> Using) => await _RemoveUsing(Using);
    [PublicAPI]public async Task<int> RemoveUsing(params string[] Using) => await _RemoveUsing(Using);
    private async Task<int> _RemoveUsing(IEnumerable<string?>? Using)
    {
        if (!CheckReady())
            return -1;

        int count = 0;
        await Semaphore.WaitAsync();
        if (Using is not null)
            foreach (string? @using in Using)
                if (@using is not null)
                {
                    UsingNoLock.Remove(UnifyUsing(@using));
                    count++;
                }
        Semaphore.Release();
        return count;
    }

    #endregion

    #region CheckReady

    private bool CheckReady()
    {
        if (IsReady)
            return true;
        
        Player.SendErrorMessage($"[{nameof(CSharpScripting)}] Environment is busy, try again later.");
        return false;
    }

    #endregion
    #region UnifyUsing

    private static readonly Regex WHITESPACE_REGEX = new(@"\s+");
    private static string UnifyUsing(string Using)
    {
        Using = Using.Trim();
        if (!Using.StartsWith("using"))
            Using = $"using {Using}";
        if (!Using.EndsWith(";"))
            Using = $"{Using};";
        Using = WHITESPACE_REGEX.Replace(Using, string.Empty, int.MaxValue, 6);
        return Using;
    }

    #endregion
    #region GetInLock

    private T GetInLock<T>(Func<T> Getter)
    {
        Semaphore.Wait();
        T ret = Getter();
        Semaphore.Release();
        return ret;
    }

    #endregion
    #region SetInLock

    private void SetInLock(Action Setter)
    {
        Semaphore.Wait();
        Setter();
        Semaphore.Release();
    }

    #endregion
}