using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;

namespace Hypernex.CCK.GodotVersion
{
    public partial class PluginLoader : Node
    {
        private static readonly List<HypernexPlugin> _loadedPlugins = new List<HypernexPlugin>();
        public static List<HypernexPlugin> LoadedPlugins => new List<HypernexPlugin>(_loadedPlugins);

        public static int LoadAllPlugins(string path)
        {
            int pluginsLoaded = 0;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return 0;
            }
            foreach (string possiblePluginFile in Directory.GetFiles(path))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFile(Path.GetFullPath(possiblePluginFile));
                    List<Type> plugins = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(HypernexPlugin)))
                        .ToList();
                    foreach (Type pluginType in plugins)
                    {
                        HypernexPlugin hypernexPlugin = (HypernexPlugin) Activator.CreateInstance(pluginType);
                        if (hypernexPlugin == null)
                            throw new Exception("Failed to create instance from HypernexPlugin!");
                        Logger.CurrentLogger.Log($"Loaded Plugin {hypernexPlugin.PluginName} by {hypernexPlugin.PluginCreator} ({hypernexPlugin.PluginVersion})");
                        _loadedPlugins.Add(hypernexPlugin);
                        pluginsLoaded++;
                        hypernexPlugin.OnPluginLoaded();
                    }
                }
                catch (Exception e)
                {
                    Logger.CurrentLogger.Error("Failed to Load Plugin at " + possiblePluginFile + " for reason " + e);
                }
            }
            return pluginsLoaded;
        }

        public override void _Ready()
        {
            foreach (HypernexPlugin hypernexPlugin in LoadedPlugins)
                hypernexPlugin.Start();
        }

        public override void _PhysicsProcess(double delta)
        {
            foreach (HypernexPlugin hypernexPlugin in LoadedPlugins)
                hypernexPlugin.FixedUpdate();
        }

        public override void _Process(double delta)
        {
            foreach (HypernexPlugin hypernexPlugin in LoadedPlugins)
                hypernexPlugin.Update();
        }

        public override void _ExitTree()
        {
            foreach (HypernexPlugin hypernexPlugin in LoadedPlugins)
            {
                hypernexPlugin.OnApplicationExit();
                _loadedPlugins.Remove(hypernexPlugin);
            }
        }
    }
}
