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
        public Dictionary<string, WorldComponentConverter> componentConverters = new Dictionary<string, WorldComponentConverter>();
        public Dictionary<string, WorldAssetConverter> assetConverters = new Dictionary<string, WorldAssetConverter>();

        public override void _Ready()
        {
            Instance = this;
            ScanForClasses(GetType().Assembly.GetTypes());
        }

        public void ScanForClasses(Type[] types)
        {
            foreach (var type in types)
            {
                if (type.IsAssignableTo(typeof(WorldComponentConverter)) && !type.IsAbstract)
                {
                    string name = type.GetCustomAttribute<ComponentConverterNameAttribute>()?.Name ?? type.Name;
                    WorldComponentConverter inst = (WorldComponentConverter)type.GetConstructor(Array.Empty<Type>()).Invoke(Array.Empty<object>());
                    componentConverters.Add(name, inst);
                }
                if (type.IsAssignableTo(typeof(WorldAssetConverter)) && !type.IsAbstract)
                {
                    string name = type.GetCustomAttribute<AssetConverterNameAttribute>()?.Name ?? type.Name;
                    WorldAssetConverter inst = (WorldAssetConverter)type.GetConstructor(Array.Empty<Type>()).Invoke(Array.Empty<object>());
                    assetConverters.Add(name, inst);
                }
            }
        }

        public bool GetAssetConverter(Type t, out string name, out WorldAssetConverter converter)
        {
            name = assetConverters.FirstOrDefault(x => x.Value.CanHandleType(t)).Key;
            converter = assetConverters.FirstOrDefault(x => x.Value.CanHandleType(t)).Value;
            return name != null && converter != null;
        }

        public bool GetComponentConverter(Type t, out string name, out WorldComponentConverter converter)
        {
            name = componentConverters.FirstOrDefault(x => x.Value.CanHandleType(t)).Key;
            converter = componentConverters.FirstOrDefault(x => x.Value.CanHandleType(t)).Value;
            return name != null && converter != null;
        }

        public WorldAsset LoadAsset(WorldData data, WorldDataAsset dataObject)
        {
            if (assetConverters.TryGetValue(dataObject.ClassName, out var converter))
            {
                return converter.LoadFromData(data, dataObject.ClassData);
            }
            return null;
        }

        public WorldDataAsset SaveAsset(WorldData data, WorldAsset obj)
        {
            if (GetAssetConverter(obj.GetType(), out var name, out var converter))
            {
                var dataObject = new WorldDataAsset();
                // dataObject.Path = $"{obj.ResourcePath}:{obj.ResourceName}";
                dataObject.Path = obj.Path;
                dataObject.ClassName = name;
                dataObject.ClassData = converter.SaveToData(data, obj);
                return dataObject;
            }
            return null;
        }

        public Node LoadComponent(WorldData data, WorldDataComponent dataObject)
        {
            if (componentConverters.TryGetValue(dataObject.ClassName, out var converter))
            {
                return converter.LoadFromData(data, dataObject.ClassData);
            }
            return null;
        }

        public WorldDataComponent SaveComponent(WorldData data, Node obj)
        {
            if (GetComponentConverter(obj.GetType(), out var name, out var converter))
            {
                var dataObject = new WorldDataComponent();
                dataObject.ClassName = name;
                dataObject.ClassData = converter.SaveToData(data, obj);
                return dataObject;
            }
            return null;
        }

        public WorldObject LoadObject(WorldData data, WorldDataObject dataObject)
        {
            WorldObject worldObject = new WorldObject();
            worldObject.Name = dataObject.Name;
            worldObject.LoadFromData(dataObject.ClassData);
            return worldObject;
        }

        public WorldDataObject SaveObject(WorldData data, WorldObject obj)
        {
            var dataObject = new WorldDataObject();
            dataObject.Name = obj.Name;
            dataObject.ParentObject = obj.GetParentIndex();
            foreach (var cmp in obj.Components)
            {
                dataObject.Components.Add(data.AllComponents.Count);
                data.AllComponents.Add(SaveComponent(data, cmp));
            }
            dataObject.ClassName = nameof(Node3D);
            dataObject.ClassData = obj.SaveToData();
            return dataObject;
        }

        public WorldRoot LoadWorld(WorldData data)
        {
            WorldRoot root = new WorldRoot();
            foreach (var obj in data.AllAssets)
            {
                var o = LoadAsset(data, obj);
                root.AddAsset(o);
            }
            foreach (var obj in data.AllComponents)
            {
                var o = LoadComponent(data, obj);
                root.AddComponent(o);
            }
            foreach (var obj in data.AllObjects)
            {
                var o = LoadObject(data, obj);
                o.Components.AddRange(obj.Components.Select(x => root.Components[x] as Node3D));
                o._parent = obj.ParentObject;
                root.AddObject(o);
            }
            return root;
        }

        public WorldData SaveWorld(WorldRoot root)
        {
            WorldData data = new WorldData();
            foreach (var obj in root.Assets)
            {
                data.AllAssets.Add(SaveAsset(data, obj));
            }
            foreach (var obj in root.Objects)
            {
                data.AllObjects.Add(SaveObject(data, obj));
            }
            return data;
        }

        private void ConvertTree(WorldData data, Node node, int parent)
        {
            if (WorldObject.IsComponentRoot(node) && node is Node3D n)
            {
                var dataObject = new WorldDataObject();
                dataObject.Name = node.Name;
                dataObject.ParentObject = parent;
                dataObject.ClassName = nameof(Node3D);
                dataObject.ClassData = WorldObject.SaveToData(n);
                dataObject.Components.Add(data.AllComponents.Count);
                data.AllComponents.Add(SaveComponent(data, node));
                parent = data.AllObjects.Count;
                data.AllObjects.Add(dataObject);
            }
            else if (parent != -1)
            {
                var dataObject = data.AllObjects[parent];
                dataObject.Components.Add(data.AllComponents.Count);
                data.AllComponents.Add(SaveComponent(data, node));
            }
            foreach (var child in node.GetChildren())
            {
                ConvertTree(data, child, parent);
            }
        }

        public WorldData ConvertWorld(Node node)
        {
            WorldData data = new WorldData();
            foreach (var child in node.GetChildren())
            {
                int parent = -1;
                ConvertTree(data, child, parent);
            }
            return data;
        }

        public void PostLoad(WorldRoot worldRoot)
        {
        }

        public void PreSave(WorldRoot worldRoot)
        {
        }

        public static WorldRoot LoadWorld(WorldData worldData, Action<Node> spawnCallback = null)
        {
            try
            {
                WorldRoot node = Instance.LoadWorld(worldData);
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
