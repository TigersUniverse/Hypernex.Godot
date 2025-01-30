using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using Godot;
using Hypernex;
using Hypernex.CCK;
using Hypernex.CCK.GodotVersion;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.Configuration;
using Hypernex.Game;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using Hypernex.UI;
using HypernexSharp.APIObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;

public partial class Init : Node
{
    public static System.Net.Http.HttpClient http = null;
    public static Init Instance;
    [Export]
    public LoginScreen login;
    [Export]
    public MainOverlay overlay;
    [Export]
    public LoadingOverlay loadingOverlay;
    [Export]
    public Control ui;
    [Export]
    public CanvasLayer uiLayer;
    [Export]
    public PackedScene localPlayerScene;
    [Export]
    public PackedScene remotePlayerScene;
    [Export]
    public PackedScene vrRigScene;

    public VRRig vrRig;

    public static Node worldsRoot;
    private static IntPtr fliteLibHandle = IntPtr.Zero;

    public static XRInterface xrInterface;
    public static bool IsVRLoaded => xrInterface != null && xrInterface.IsInitialized();

    public static bool resolverSet = false;

    public static Func<ISceneProvider> WorldProvider;
    public static Func<ISceneProvider> AvatarProvider;

    static Init()
    {
        if (resolverSet)
            return;
        resolverSet = true;
        NativeLibrary.SetDllImportResolver(typeof(Init).Assembly, Resolver);
        string dir = OS.GetExecutablePath().GetBaseDir();
        if (OS.HasFeature("editor"))
        {
            dir = Path.Combine(Directory.GetCurrentDirectory(), "export_data", OS.GetName().ToLower());
        }
        DynamicallyLoadedBindings.LibrariesPath = dir;
        DynamicallyLoadedBindings.Initialize();
    }

    public override void _Ready()
    {
        Instance = this;

        GDLogger logger = new GDLogger();
        logger.SetLogger();
        kcp2k.Log.Info = s => logger.Debug(s);
        kcp2k.Log.Warning = s => logger.Warn(s);
        kcp2k.Log.Error = s => logger.Error(s);
        Telepathy.Log.Info = s => logger.Debug(s);
        Telepathy.Log.Warning = s => logger.Warn(s);
        Telepathy.Log.Error = s => logger.Error(s);
        SafeLoader.Log = s => logger.Debug(s);
        SafeLoader.LogError = s => logger.Error(s);

        GetTree().Root.SizeChanged += Resized;
        Resized();

        http = new System.Net.Http.HttpClient();

        // this is only needed on android, and exposes MITM attacks
        if (OS.GetName().Equals("Android", StringComparison.OrdinalIgnoreCase))
        {
            http = new System.Net.Http.HttpClient(new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true,
            });
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        }

        typeof(HypernexSharp.HypernexSettings).Assembly.GetType("HypernexSharp.API.HTTPTools").GetField("_client", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, http);

        NativeLibrary.SetDllImportResolver(typeof(Flite.FliteNativeApi).Assembly, FliteResolver);
        // NativeLibrary.SetDllImportResolver(typeof(Discord.Discord).Assembly, DiscordResolver);

        AddChild(new QuickInvoke() { Name = "QuickInvoke" });
        AddChild(new ConfigManager() { Name = "ConfigManager" });
        GameInstance.OnGameInstanceLoaded += GameInstanceLoaded;
        worldsRoot = new Node() { Name = "Worlds" };
        AddChild(worldsRoot);
        DownloadTools.DownloadsPath = Path.Combine(OS.GetUserDataDir(), "Downloads");

        SetupSceneProviders();

        int pluginsLoaded;
        try
        {
            pluginsLoaded = PluginLoader.LoadAllPlugins(Path.Combine(OS.GetUserDataDir(), "Plugins"));
        }
        catch (Exception)
        {
            pluginsLoaded = 0;
        }
        Logger.CurrentLogger.Log($"Loaded {pluginsLoaded} Plugins!");
        AddChild(new PluginLoader() { Name = "PluginLoader" });

        // AddChild(new DiscordGDTools() { Name = "DiscordGDTools" });

        UpdateYtDl();

        SetupAndRunGame();

        // GD.Print(string.Join(" ", GetValidClasses()));
    }

    private void Resized()
    {
        // GetTree().Root.ContentScaleSize = new Vector2I(1280, 720);
    }

    public override void _Process(double delta)
    {
        GameInstance.FocusedInstance?.Update();
        GameInstance.FocusedInstance?.LateUpdate();
    }

    public override void _PhysicsProcess(double delta)
    {
        GameInstance.FocusedInstance?.FixedUpdate();
    }

    private static void GameInstanceLoaded(GameInstance instance, WorldMeta meta)
    {
        if (instance.IsDisposed)
        {
            Instance.ui.Show();
        }
        else
        {
            instance.World.Load();
            instance.World.Name = instance.instanceId;
            worldsRoot.AddChild(instance.World, true);
            var plr = NewPlayer(true);
            if (APITools.CurrentUser != null)
                plr.SetUser(APITools.CurrentUser.Id, instance);
            else
                plr.SetUserOffline(instance);
            instance.World.AddPlayer(plr);
            // Instance.ui.Hide();
        }
    }

    public static List<string> GetValidClasses()
    {
        List<string> list = new List<string>(ClassDB.GetClassList().Where(x => ClassDB.IsParentClass(x, nameof(Node)) || ClassDB.IsParentClass(x, nameof(Resource))).Select(x => x.ToLower()));
        // Viewport/Window classes
        list.Remove(nameof(StatusIndicator).ToLower());
        list.Remove(nameof(Viewport).ToLower());
        list.Remove(nameof(Window).ToLower());
        list.Remove(nameof(AcceptDialog).ToLower());
        list.Remove(nameof(ConfirmationDialog).ToLower());
        list.Remove(nameof(FileDialog).ToLower());
        list.Remove(nameof(Popup).ToLower());
        list.Remove(nameof(PopupMenu).ToLower());
        list.Remove(nameof(PopupPanel).ToLower());
        // PackedScene and Scripts
        list.Remove(nameof(PackedScene).ToLower());
        list.Remove(nameof(Script).ToLower());
        list.Remove(nameof(GDScript).ToLower());
        list.Remove(nameof(CSharpScript).ToLower());
        // XR/3D
        list.Remove(nameof(XRCamera3D).ToLower());
        list.Remove(nameof(XROrigin3D).ToLower());
        list.Remove(nameof(XRNode3D).ToLower());
        list.Remove(nameof(XRAnchor3D).ToLower());
        list.Remove(nameof(XRController3D).ToLower());
        list.Remove(nameof(XRBodyModifier3D).ToLower());
        list.Remove(nameof(XRFaceModifier3D).ToLower());
        return list;
    }

    public static PlayerRoot NewPlayer(bool isLocal)
    {
        if (isLocal)
            return Instance.localPlayerScene.Instantiate<PlayerRoot>();
        else
            return Instance.remotePlayerScene.Instantiate<PlayerRoot>();
    }

    private static IntPtr FliteResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != Flite.FliteNativeApi.LibName)
            return IntPtr.Zero;
        if (fliteLibHandle != IntPtr.Zero)
            return fliteLibHandle;
        string dir = Directory.GetCurrentDirectory();
        if (OS.HasFeature("editor"))
        {
            dir = Path.Combine(dir, "export_data", OS.GetName().ToLower());
        }
        switch (OS.GetName().ToLower())
        {
            case "windows":
                fliteLibHandle = NativeLibrary.Load(Path.Combine(dir, $"{Flite.FliteNativeApi.LibName}.dll"));
                break;
            case "linux":
                fliteLibHandle = NativeLibrary.Load(Path.Combine(dir, $"lib{Flite.FliteNativeApi.LibName}.so"));
                break;
            default:
                return IntPtr.Zero;
        }
        return fliteLibHandle;
    }

    public static string GetLibPath(string libraryName)
    {
        string dir = OS.GetExecutablePath().GetBaseDir();
        if (OS.HasFeature("editor"))
        {
            dir = Path.Combine(Directory.GetCurrentDirectory(), "export_data", OS.GetName().ToLower());
        }
        string libHandle = string.Empty;
        switch (OS.GetName().ToLower())
        {
            case "windows":
                if (libraryName.Contains(".dll"))
                    libHandle = Path.Combine(dir, libraryName);
                else
                    libHandle = Path.Combine(dir, $"{libraryName}.dll");
                break;
            case "linux":
            case "android":
                if (libraryName.Contains(".so"))
                    libHandle = Path.Combine(dir, libraryName);
                else
                    libHandle = Path.Combine(dir, $"lib{libraryName}.so");
                break;
        }
        return libHandle;
    }

    public static IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        string dir = OS.GetExecutablePath().GetBaseDir();
        if (OS.HasFeature("editor"))
        {
            dir = Path.Combine(Directory.GetCurrentDirectory(), "export_data", OS.GetName().ToLower());
        }
        IntPtr libHandle = IntPtr.Zero;
        switch (OS.GetName().ToLower())
        {
            case "windows":
                if (!NativeLibrary.TryLoad(Path.Combine(dir, libraryName), out libHandle))
                    libHandle = IntPtr.Zero;
                break;
            case "linux":
            case "android":
                if (!NativeLibrary.TryLoad(Path.Combine(dir, libraryName), out libHandle))
                    libHandle = IntPtr.Zero;
                break;
        }
        return libHandle;
    }

    public void InitXR()
    {
        xrInterface = XRServer.FindInterface("OpenXR");
        if (xrInterface != null && xrInterface.IsInitialized())
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            if (xrInterface is OpenXRInterface openxr)
            {
                Engine.PhysicsTicksPerSecond = (int)openxr.DisplayRefreshRate;
            }
        }
    }
    
    public void SetupSceneProviders()
    {
        WorldProvider = () =>
        {
            SafeLoader loader = new SafeLoader();
            loader.allowedClasses = GetValidClasses();
            loader.validScripts.Add(WorldDescriptor.TypeName, SafeLoader.LoadScript<WorldDescriptor>());
            loader.validScripts.Add(WorldScript.TypeName, SafeLoader.LoadScript<WorldScript>());
            loader.validScripts.Add(ReverbZone.TypeName, SafeLoader.LoadScript<ReverbZone>());
            loader.validScripts.Add(UICanvas.TypeName, SafeLoader.LoadScript<UICanvas>());
            loader.validScripts.Add(VideoPlayer.TypeName, SafeLoader.LoadScript<VideoPlayer>());
            loader.validScripts.Add(WorldAsset.TypeName, SafeLoader.LoadScript<WorldAsset>());
            loader.validScripts.Add(Mirror.TypeName, SafeLoader.LoadScript<Mirror>());
            return loader;
        };
        AvatarProvider = () =>
        {
            SafeLoader loader = new SafeLoader();
            loader.allowedClasses = GetValidClasses();
            loader.validScripts.Add(AvatarDescriptor.TypeName, SafeLoader.LoadScript<AvatarDescriptor>());
            return loader;
        };
    }

    public void OnLogin(User user)
    {
        if (user != null)
        {
            ConfigManager.SelectedConfigUser = ConfigManager.LoadedConfig.GetConfigUserFromUserId(user.Id);
            ConfigManager.SaveConfigToFile();
        }
        ui.Show();
        login.root.Hide();
        overlay.root.Show();
        overlay.ShowHome();
        if (user != null)
        {
            APITools.CreateUserSocket(SocketManager.InitSocket);
            GetWindow().Title = $"Hypernex ({user.GetUsersName()} - {APITools.APIObject.Settings.TargetDomain})";
        }
        else
        {
            GetWindow().Title = $"Hypernex (LAN)";
        }
    }

    public void UpdateYtDl()
    {
        YoutubeDLSharp.Utils.DownloadYtDlp(OS.GetUserDataDir());
        var ytdlPath = Path.Combine(OS.GetUserDataDir(), "yt-dlp");
        if (OS.GetName() == "Windows")
            ytdlPath = Path.Combine(OS.GetUserDataDir(), "yt-dlp.exe");
        else
        {
            var flags = Godot.FileAccess.GetUnixPermissions(ytdlPath);
            Godot.FileAccess.SetUnixPermissions(ytdlPath, flags | Godot.FileAccess.UnixPermissionFlags.ExecuteOwner);
        }
        Streaming.ytdl.YoutubeDLPath = ytdlPath;
        Streaming.ytdl.OutputFolder = Path.Combine(OS.GetUserDataDir(), "Downloads");
        if (!Directory.Exists(Streaming.ytdl.OutputFolder))
            Directory.CreateDirectory(Streaming.ytdl.OutputFolder);
    }

    public void SetupAndRunGame()
    {
        APITools.OnUserLogin += OnLogin;
        APITools.OnLogout += () =>
        {
            APITools.DisposeUserSocket();
            ui.Show();
            overlay.root.Hide();
            login.root.Show();
            GetWindow().Title = "Hypernex";
            ConfigManager.SaveConfigToFile();
        };

        InitXR();
        if (IsVRLoaded)
        {
            vrRig = vrRigScene.Instantiate<VRRig>();
            AddChild(vrRig);
        }

        ui.Theme = ThemeManager.Instance.realTheme;
        ui.Show();
        overlay.root.Hide();
        login.root.Show();
        GetWindow().Title = "Hypernex";
    }
}
