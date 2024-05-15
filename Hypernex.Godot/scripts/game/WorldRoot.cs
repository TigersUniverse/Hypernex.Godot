using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.CCK;
using Hypernex.CCK.GodotVersion;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.CCK.GodotVersion.Converters;
using Hypernex.Tools;

namespace Hypernex.Game
{
    public partial class WorldRoot : Node
    {
        public GameInstance gameInstance;
        public WorldDescriptor descriptor;
        public List<Node> Objects = new List<Node>();
        public List<Resource> Assets = new List<Resource>();

        public void AddPlayer(PlayerRoot player)
        {
            player.Pos = descriptor.StartPosition;
            AddChild(player);
        }

        public void AddObject(Node worldObject)
        {
            if (Objects.Contains(worldObject))
                return;
            if (worldObject is WorldDescriptor desc)
                descriptor = desc;
            worldObject.Owner = this;
            Objects.Add(worldObject);
            foreach (var child in worldObject.GetChildren())
            {
                AddObject(child);
            }
        }

        public void AddAsset(Resource worldObject)
        {
            Assets.Add(worldObject);
        }

        public void Load()
        {
            Logger.CurrentLogger.Log("World Load");
        }

        public static WorldRoot LoadFromFile(string path)
        {
            WorldRoot root = new WorldRoot();
            SafeScene safeScn = new SafeScene();
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            safeScn.FromString(file.GetAsText(true));
            ConvertDB db = new ConvertDB();
            db.Register<CSharpScriptConverter>();
            db.Register<MeshConverter>();
            db.Register<ConcaveConverter>();
            db.Register<MaterialConverter>();
            db.Register<Texture2DConverter>();
            PackedScene scn = safeScn.SetupToPackedScene(db);
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
            foreach (var child in root.FindChildren("*", owned: true))
            {
                child.Owner = root;
            }
            PackedScene scn = new PackedScene();
            scn.Pack(root);
            SafeScene safeScene = new SafeScene();
            ConvertDB db = new ConvertDB();
            db.Register<CSharpScriptConverter>();
            db.Register<ConcaveConverter>();
            db.Register<MeshConverter>();
            db.Register<MaterialConverter>();
            db.Register<Texture2DConverter>();
            safeScene.SetupFromState(scn.GetState(), db);
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            file.StoreString(safeScene.ToString());
        }
    }
}
