using Godot;
using Hypernex;
using Hypernex.CCK;
using Hypernex.CCK.Godot;
using Hypernex.Configuration;
using Hypernex.Game;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using Hypernex.UI;
using HypernexSharp.APIObjects;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

public partial class Init : Node
{
    public static Init Instance;
    [Export]
    public LoginScreen login;
    [Export]
    public MainOverlay overlay;
    [Export]
    public PackedScene localPlayerScene;
    [Export]
    public PackedScene remotePlayerScene;

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

        NativeLibrary.SetDllImportResolver(typeof(Discord.Discord).Assembly, DiscordResolver);

        AddChild(new QuickInvoke() { Name = "QuickInvoke" });
        AddChild(new ConfigManager() { Name = "ConfigManager" });
        AddChild(NewWorldManager());
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

        APITools.OnUserRefresh += user =>
        {
            login.root.Hide();
            overlay.root.Show();
            overlay.ShowHome();
            APITools.CreateUserSocket(SocketManager.InitSocket);
        };
        APITools.OnLogout += () =>
        {
            APITools.DisposeUserSocket();
            overlay.root.Hide();
            login.root.Show();
        };

        SetupAndRun();
    }

    private static WorldManager NewWorldManager()
    {
        var node = new WorldManager();
        node.Name = "Worlds";
        GameInstance.OnGameInstanceLoaded = GameInstanceLoaded;
        return node;
    }

    private static void GameInstanceLoaded(GameInstance instance, WorldMeta meta)
    {
        WorldManager.Instance.AddChild(instance.World);
    }

    public static PlayerRoot NewPlayer(bool isLocal)
    {
        if (isLocal)
            return Instance.localPlayerScene.Instantiate<PlayerRoot>();
        else
            return Instance.remotePlayerScene.Instantiate<PlayerRoot>();
    }

    private static IntPtr DiscordResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "discord_game_sdk")
        {
            return IntPtr.Zero;
        }
        string dir = Directory.GetCurrentDirectory();
        if (EngineDebugger.IsActive())
        {
            dir = Path.Combine(dir, "scripts", "plugins", "DiscordGameSDK");
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

    public void SetupAndRun()
    {
        overlay.root.Hide();
        login.root.Show();
        login.TryLoginWith();
    }
}
