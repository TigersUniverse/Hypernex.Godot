using Godot;
using Hypernex.CCK;
using Hypernex.CCK.Godot;
using Hypernex.Configuration;
using Hypernex.Tools;
using Hypernex.UI;
using HypernexSharp;
using System;
using System.IO;

public partial class Init : Node
{
    public static Init Instance;
    public HypernexObject hypernex;
    [Export]
    public LoginScreen login;
    [Export]
    public MainOverlay overlay;

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

        AddChild(new QuickInvoke());
        AddChild(new ConfigManager());
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
        AddChild(new PluginLoader());

        login.OnUser += (s, e) =>
        {
            login.root.Hide();
            overlay.root.Show();
        };
        overlay.OnLogout += (s, e) =>
        {
            overlay.root.Hide();
            login.root.Show();
        };

        SetupAndRun();
    }

    public void SetupAndRun()
    {
        overlay.root.Hide();
        login.root.Show();
        login.TryLoginWith();
    }
}
