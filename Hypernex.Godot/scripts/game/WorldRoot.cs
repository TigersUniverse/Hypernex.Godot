using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.CCK;
using Hypernex.CCK.GodotVersion;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.CCK.GodotVersion.Converters;
using Hypernex.Player;
using Hypernex.Sandboxing;
using Hypernex.Tools;

namespace Hypernex.Game
{
    public partial class WorldRoot : Node
    {
        public ISceneProvider safeLoader;
        public GameInstance gameInstance;
        public WorldDescriptor descriptor;
        public List<Node> Objects = new List<Node>();
        public List<ScriptRunner> Runners = new List<ScriptRunner>();
        public List<WorldAsset> Assets = new List<WorldAsset>();

        public void AddPlayer(PlayerRoot player)
        {
            AddChild(player);
            RespawnPlayer(player);
            if (player.IsLocal)
            {
                foreach (Mirror mirror in Objects.Where(x => x is Mirror))
                {
                    if (IsInstanceValid(mirror))
                        mirror.realCamera = Init.IsVRLoaded ? Init.Instance.vrRig.head : player.GetViewport().GetCamera3D();
                }
            }
        }

        public void RespawnPlayer(PlayerRoot player)
        {
            if (IsInstanceValid(descriptor))
                player.Pos = descriptor.GetRandomSpawn().GlobalPosition;
            else
            {
                var query = new PhysicsRayQueryParameters3D()
                {
                    From = Vector3.Up * 1000f,
                    To = Vector3.Down * 1000f,
                };
                var results = player.GetWorld3D().DirectSpaceState.IntersectRay(query);
                if (results.Count == 0)
                    player.GetPart<PlayerInputs>().isNoclip = true;
                else
                    player.Pos = results["position"].AsVector3();
            }
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
            if (worldObject is Mirror mirror)
            {
                mirror.CallbackGetNodes = () => Objects;
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
            if (worldObject is Light3D light && OS.GetName().Equals("Android", StringComparison.OrdinalIgnoreCase))
            {
                light.ShadowEnabled = false;
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

        public void Unload()
        {
            gameInstance = null;
            foreach (var obj in Objects)
                if (IsInstanceValid(obj))
                    obj.Free();
            safeLoader.Dispose();
        }

        public static WorldRoot LoadFromFile(string path)
        {
            WorldRoot root = new WorldRoot();
            ISceneProvider loader = Init.WorldProvider();
            root.safeLoader = loader;
            PackedScene scn = null;
            if (IsInstanceValid(Init.Instance))
            {
                QuickInvoke.InvokeActionOnMainThread(() =>
                {
                    Init.Instance.loadingOverlay.isLoading++;
                });
            }
            try
            {
                scn = loader.LoadFromFile(path);
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
            if (!IsInstanceValid(scn))
            {
                Logger.CurrentLogger.Error("Unable to load world!");
                loader.Dispose();
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
