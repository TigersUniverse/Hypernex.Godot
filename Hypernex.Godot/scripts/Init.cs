using Godot;
using Hypernex;
using Hypernex.CCK;
using Hypernex.CCK.GodotVersion;
using Hypernex.Configuration;
using Hypernex.Game;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using Hypernex.UI;
using HypernexSharp.APIObjects;
using System;
using System.IO;
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

    static Init()
    {
        if (resolverSet)
            return;
        resolverSet = true;
        NativeLibrary.SetDllImportResolver(typeof(Init).Assembly, Resolver);
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

        http = new System.Net.Http.HttpClient();

        // this is only needed on android, and exposes MITM attacks
        /*
        {
            http = new System.Net.Http.HttpClient(new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true,
            });
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        }
        */

        typeof(HypernexSharp.HypernexSettings).Assembly.GetType("HypernexSharp.API.HTTPTools").GetField("_client", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, http);

        NativeLibrary.SetDllImportResolver(typeof(Flite.FliteNativeApi).Assembly, FliteResolver);
        // NativeLibrary.SetDllImportResolver(typeof(Discord.Discord).Assembly, DiscordResolver);

        AddChild(new QuickInvoke() { Name = "QuickInvoke" });
        AddChild(new ConfigManager() { Name = "ConfigManager" });
        GameInstance.OnGameInstanceLoaded += GameInstanceLoaded;
        worldsRoot = new Node() { Name = "Worlds" };
        AddChild(worldsRoot);
        DownloadTools.DownloadsPath = Path.Combine(OS.GetUserDataDir(), "Downloads");

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

        AddChild(new DiscordGDTools() { Name = "DiscordGDTools" });

        SetupAndRunGame();
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
            worldsRoot.AddChild(instance.World);
            var plr = NewPlayer(true);
            plr.SetUser(APITools.CurrentUser.Id, instance);
            instance.World.AddPlayer(plr);
            // Instance.ui.Hide();
        }
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
            dir = Path.Combine(dir, "addons");
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

    public static IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        string dir = OS.GetExecutablePath().GetBaseDir();
        if (OS.HasFeature("editor"))
        {
            dir = Path.Combine(Directory.GetCurrentDirectory(), "addons", "natives");
        }
        IntPtr libHandle = IntPtr.Zero;
        switch (OS.GetName().ToLower())
        {
            case "windows":
                if (libraryName.Contains(".dll"))
                    libHandle = NativeLibrary.Load(Path.Combine(dir, libraryName));
                else
                    libHandle = NativeLibrary.Load(Path.Combine(dir, $"{libraryName}.dll"));
                break;
            case "linux":
                if (libraryName.Contains(".so"))
                    libHandle = NativeLibrary.Load(Path.Combine(dir, libraryName));
                else
                    libHandle = NativeLibrary.Load(Path.Combine(dir, $"lib{libraryName}.so"));
                break;
        }
        return libHandle;
    }

    private static IntPtr DiscordResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "discord_game_sdk")
        {
            return IntPtr.Zero;
        }
        string dir = Directory.GetCurrentDirectory();
        if (OS.HasFeature("editor"))
        {
            dir = Path.Combine(dir, "addons", "DiscordGameSDK");
        }
        switch (OS.GetName().ToLower())
        {
            case "windows":
                return NativeLibrary.Load(Path.Combine(dir, "discord_game_sdk.dll"));
            case "linux":
                return NativeLibrary.Load(Path.Combine(dir, "discord_game_sdk.so"));
            default:
                return IntPtr.Zero;
        }
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

    public void SetupAndRunGame()
    {
        APITools.OnUserLogin += user =>
        {
            ConfigManager.SelectedConfigUser = ConfigManager.LoadedConfig.GetConfigUserFromUserId(user.Id);
            ui.Show();
            login.root.Hide();
            overlay.root.Show();
            overlay.ShowHome();
            APITools.CreateUserSocket(SocketManager.InitSocket);
            GetWindow().Title = $"Hypernex ({user.GetUsersName()} - {APITools.APIObject.Settings.TargetDomain})";
        };
        APITools.OnLogout += () =>
        {
            APITools.DisposeUserSocket();
            ui.Show();
            overlay.root.Hide();
            login.root.Show();
            GetWindow().Title = "Hypernex";
        };

        InitXR();
        if (IsVRLoaded)
        {
            vrRig = vrRigScene.Instantiate<VRRig>();
            AddChild(vrRig);
        }

        ui.Show();
        overlay.root.Hide();
        login.root.Show();
        GetWindow().Title = "Hypernex";
    }
}
