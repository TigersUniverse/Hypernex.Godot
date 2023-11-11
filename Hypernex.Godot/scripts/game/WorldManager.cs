using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using Hypernex.Game.Classes;
using Hypernex.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

namespace Hypernex.Game
{
    public partial class WorldManager : Node
    {
        public static WorldManager Instance;
        public Dictionary<string, Type> classes = new Dictionary<string, Type>();

        public override void _Ready()
        {
            Instance = this;
            classes.Clear();
            ScanForClasses(GetType().Assembly.GetTypes());
        }

        public void ScanForClasses(Type[] types)
        {
            foreach (var type in types)
            {
                PropertyInfo prop = type.GetProperty(IWorldClass.ClassNameName, BindingFlags.Public | BindingFlags.Static);
                if (type.GetInterfaces().Contains(typeof(IWorldClass)) && prop != null && prop.PropertyType == typeof(string))
                {
                    classes.Add((string)prop.GetValue(null), type);
                }
            }
        }

        private bool IsValidWorldObject(Node node)
        {
            return classes.ContainsValue(node.GetType());
        }

        private WorldObject ToWorldObject(Node node)
        {
            WorldObject obj = new WorldObject();
            obj.Name = node.Name;
            obj.ClassName = classes.FirstOrDefault(x => x.Value == node.GetType()).Key;
            if (!string.IsNullOrEmpty(obj.ClassName) && node is IWorldClass cl)
            {
                // obj.ClassData = cl.SaveToData();
                obj.ClassData2 = JObject.Parse(cl.SaveToData());
            }
            foreach (var child in node.GetChildren())
            {
                if (IsValidWorldObject(child))
                {
                    obj.ChildObjects.Add(ToWorldObject(child));
                }
            }
            return obj;
        }

        private Node FromWorldObject(WorldObject obj)
        {
            Node node = null;
            if (classes.TryGetValue(obj.ClassName, out var type))
            {
                IWorldClass cl = (IWorldClass)type.GetConstructor(new Type[0]).Invoke(new object[0]);
                if (cl is Node)
                {
                    node = cl as Node;
                    node.Name = obj.Name;
                    // cl.LoadFromData(obj.ClassData);
                    cl.LoadFromData(obj.ClassData2.ToString(Formatting.None));
                    foreach (var child in obj.ChildObjects)
                    {
                        node.AddChild(FromWorldObject(child));
                    }
                }
            }
            return node;
        }

        public Node LoadWorld(WorldData data)
        {
            WorldRoot worldRoot = new WorldRoot();
            foreach (var child in data.RootObjects)
            {
                worldRoot.AddChild(FromWorldObject(child));
            }
            return worldRoot;
        }

        public void PostLoad(Node worldRoot)
        {
            foreach (var node in worldRoot.GetChildren())
            {
                if (IsValidWorldObject(node) && node is IWorldClass wc)
                {
                    wc.PostLoad();
                    PostLoad(node);
                }
            }
        }

        public WorldData SaveWorld(Node worldRoot)
        {
            WorldData data = new WorldData();
            foreach (var node in worldRoot.GetChildren())
            {
                if (IsValidWorldObject(node))
                {
                    data.RootObjects.Add(ToWorldObject(node));
                }
            }
            return data;
        }

        public void PreSave(Node worldRoot)
        {
            foreach (var node in worldRoot.GetChildren())
            {
                if (IsValidWorldObject(node) && node is IWorldClass wc)
                {
                    wc.PreSave();
                    PreSave(node);
                }
            }
        }

        public static Node LoadWorld(WorldData worldData, Action<Node> spawnCallback = null)
        {
            try
            {
                Node node = Instance.LoadWorld(worldData);
                spawnCallback?.Invoke(node);
                Instance.PostLoad(node);
                return node;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static WorldData LoadFromFile(string path)
        {
            return DebugLoadFromFile();
            return JsonTools.MsgPackDeserialize<WorldData>(path);
        }

        public static WorldData DebugLoadFromFile()
        {
            string dir = Path.Combine(OS.GetUserDataDir(), "WorldSaves");
            return JsonTools.MsgPackDeserialize<WorldData>(File.ReadAllText(Path.Combine(dir, "world.msgpack")));
        }

        public static void DebugSaveToFile(WorldData data, bool json = false)
        {
            string dir = Path.Combine(OS.GetUserDataDir(), "WorldSaves");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(Path.Combine(dir, "world.msgpack"), JsonTools.MsgPackSerialize(data));
            if (json)
                File.WriteAllText(Path.Combine(dir, "world.json"), JsonTools.JsonSerialize(data));
        }
    }
}
