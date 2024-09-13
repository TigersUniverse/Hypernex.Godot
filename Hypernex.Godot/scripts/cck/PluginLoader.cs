using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Godot;

namespace Hypernex.CCK.GodotVersion
{
    public partial class PluginLoader : Node
    {
        private static readonly List<HypernexPlugin> _loadedPlugins = new List<HypernexPlugin>();
        public static List<HypernexPlugin> LoadedPlugins => new List<HypernexPlugin>(_loadedPlugins);
        private static string loadingPath;

        public static int LoadAllPlugins(string path)
        {
            loadingPath = path;
            AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
            alc.Resolving += ResolveManaged;
            int pluginsLoaded = 0;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return 0;
            }
            foreach (string possiblePluginFile in Directory.GetFiles(path))
            {
                if (!Path.GetExtension(possiblePluginFile).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    Assembly assembly = alc.LoadFromAssemblyPath(Path.GetFullPath(possiblePluginFile));
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

        private static Assembly ResolveManaged(AssemblyLoadContext context, AssemblyName name)
        {
            return context.LoadFromAssemblyPath(Path.Combine(loadingPath, name.Name + ".dll"));
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
            foreach (HypernexPlugin hypernexPlugin in LoadedPlugins)
                hypernexPlugin.LateUpdate();
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
