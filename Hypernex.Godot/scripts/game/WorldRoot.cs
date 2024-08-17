using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.CCK;
using Hypernex.CCK.GodotVersion;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.CCK.GodotVersion.Converters;
using Hypernex.Sandboxing;
using Hypernex.Tools;

namespace Hypernex.Game
{
    public partial class WorldRoot : Node
    {
        public GameInstance gameInstance;
        public WorldDescriptor descriptor;
        public List<Node> Objects = new List<Node>();
        public List<ScriptRunner> Runners = new List<ScriptRunner>();
        public List<WorldAsset> Assets = new List<WorldAsset>();

        public void AddPlayer(PlayerRoot player)
        {
            RespawnPlayer(player);
            AddChild(player);
        }

        public void RespawnPlayer(PlayerRoot player)
        {
            player.Pos = descriptor.GetRandomSpawn().GlobalPosition;
        }

        public void AddObject(Node worldObject)
        {
            if (Objects.Contains(worldObject))
                return;
            if (worldObject is WorldDescriptor desc)
            {
                descriptor = desc;
                if (descriptor.Assets != null)
                    foreach (var asset in descriptor.Assets)
                    {
                        AddAsset(asset);
                    }
            }
            if (worldObject is WorldScript script)
            {
                ScriptRunner runner = new ScriptRunner();
                runner.Name = script.Name + "_Runner";
                runner.world = this;
                runner.scriptRef = script;
                script.AddSibling(runner);
                Runners.Add(runner);
            }
            if (worldObject is AudioStreamPlayer audio)
            {
                audio.Bus = "World";
            }
            if (worldObject is AudioStreamPlayer2D audio2d)
            {
                audio2d.Bus = "World";
            }
            if (worldObject is AudioStreamPlayer3D audio3d)
            {
                audio3d.Bus = "World";
            }
            worldObject.Owner = this;
            Objects.Add(worldObject);
            foreach (var child in worldObject.GetChildren())
            {
                AddObject(child);
            }
        }

        public void AddAsset(WorldAsset worldObject)
        {
            Assets.Add(worldObject);
        }

        public T GetAsset<T>(string asset) where T : Resource
        {
            return GetAsset(asset, typeof(T)) as T;
        }

        public Resource GetAsset(string asset, Type t)
        {
            return Assets.FirstOrDefault(x => x.name == asset && t.IsAssignableFrom(x.asset.GetType())).asset;
        }

        public void Load()
        {
            Logger.CurrentLogger.Log("World Load");
        }

        public static WorldRoot LoadFromFile(string path)
        {
            WorldRoot root = new WorldRoot();
            SafeLoader loader = new SafeLoader();
            loader.validScripts.Add(WorldDescriptor.TypeName, SafeLoader.LoadScript<WorldDescriptor>());
            loader.validScripts.Add(WorldScript.TypeName, SafeLoader.LoadScript<WorldScript>());
            loader.validScripts.Add(ReverbZone.TypeName, SafeLoader.LoadScript<ReverbZone>());
            loader.validScripts.Add(UICanvas.TypeName, SafeLoader.LoadScript<UICanvas>());
            loader.validScripts.Add(VideoPlayer.TypeName, SafeLoader.LoadScript<VideoPlayer>());
            loader.validScripts.Add(WorldAsset.TypeName, SafeLoader.LoadScript<WorldAsset>());
            if (IsInstanceValid(Init.Instance))
            {
                QuickInvoke.InvokeActionOnMainThread(() =>
                {
                    Init.Instance.loadingOverlay.isLoading++;
                });
            }
            try
            {
                loader.ReadZip(path);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
            }
            if (IsInstanceValid(Init.Instance))
            {
                QuickInvoke.InvokeActionOnMainThread(() =>
                {
                    Init.Instance.loadingOverlay.isLoading--;
                });
            }
            PackedScene scn = loader.scene;
            if (!IsInstanceValid(loader.scene))
            {
                Logger.CurrentLogger.Error("Unable to load world!");
                return root;
            }
            Node node = scn.Instantiate();
            if (IsInstanceValid(node))
            {
                root.AddChild(node);
                root.AddObject(node);
            }
            return root;
        }

        public static void SaveToFile(string path, Node root)
        {
            throw new NotImplementedException();
        }
    }
}
