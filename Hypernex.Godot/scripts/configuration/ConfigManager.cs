using System;
using System.IO;
using Godot;
using Hypernex.Configuration.ConfigMeta;
using Tomlet;
using Tomlet.Models;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Configuration
{
    public partial class ConfigManager : Node
    {
        private static string persistentAppData;
    
        public static string ConfigLocation => Path.Combine(persistentAppData, "config.cfg");
        public static Config LoadedConfig;
        public static ConfigUser SelectedConfigUser;

        public static Action<Config> OnConfigSaved = config => { };
        public static Action<Config> OnConfigLoaded = config => { };

        public override void _Ready()
        {
            persistentAppData = OS.GetUserDataDir();
            LoadConfigFromFile();
        }

        public override void _ExitTree()
        {
            SaveConfigToFile(LoadedConfig);
        }

        public static void LoadConfigFromFile()
        {
            if (File.Exists(ConfigLocation))
            {
                try
                {
                    string text = File.ReadAllText(ConfigLocation);
                    LoadedConfig = TomletMain.To<Config>(text);
                    OnConfigLoaded.Invoke(LoadedConfig);
                    Logger.CurrentLogger.Debug("Loaded Config");
                }
                catch (Exception e)
                {
                    Logger.CurrentLogger.Critical(e);
                }
            }
            else
            {
                LoadedConfig = new Config();
                SaveConfigToFile();
            }
        }

        public static void SaveConfigToFile(Config config = null)
        {
            if (config == null)
                config = LoadedConfig;
            if (config == null)
                return;
            // Clone the SelectedConfigUser
            if (SelectedConfigUser != null)
            {
                ConfigUser docConfigUser = config.GetConfigUserFromUserId(SelectedConfigUser.UserId);
                if(docConfigUser != null)
                    docConfigUser.Clone(SelectedConfigUser);
            }
            TomlDocument document = TomletMain.DocumentFrom(typeof(Config), config);
            string text = document.SerializedValue;
            File.WriteAllText(ConfigLocation, text);
            OnConfigSaved.Invoke(config);
            Logger.CurrentLogger.Debug("Saved Config");
        }
    }
}
