using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using Hypernex.Game.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

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
                obj.ClassData = cl.SaveToData();
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
                    cl.LoadFromData(obj.ClassData);
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
            Node worldRoot = new Node();
            foreach (var child in data.RootObjects)
            {
                worldRoot.AddChild(FromWorldObject(child));
            }
            return worldRoot;
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

        public static WorldData LoadFromFile()
        {
            string dir = Path.Combine(OS.GetUserDataDir(), "WorldSaves");

            JsonSerializer serializer = new JsonSerializer();
            using MemoryStream ms2 = new MemoryStream(File.ReadAllBytes(Path.Combine(dir, "world.json")));
            using StreamReader sw = new StreamReader(ms2);
            using (JsonTextReader reader = new JsonTextReader(sw))
            {
                return serializer.Deserialize<WorldData>(reader);
            }
        }

        public static void SaveToFile(WorldData data)
        {
            string dir = Path.Combine(OS.GetUserDataDir(), "WorldSaves");

            JsonSerializer serializer = new JsonSerializer();
            using MemoryStream ms = new MemoryStream();
            using (BsonDataWriter writer = new BsonDataWriter(ms))
            {
                serializer.Serialize(writer, data);
            }
            using MemoryStream ms2 = new MemoryStream();
            using StreamWriter sw = new StreamWriter(ms2);
            using (JsonTextWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, data);
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(Path.Combine(dir, "world.json"), ms2.ToArray());
            File.WriteAllBytes(Path.Combine(dir, "world.bson"), ms.ToArray());
        }
    }
}