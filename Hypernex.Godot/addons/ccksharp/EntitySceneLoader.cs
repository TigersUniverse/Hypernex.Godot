using System.Collections.Generic;
using Godot;
using Hypernex.CCK.GodotVersion.AssetTypes;
using Hypernex.CCK.GodotVersion.ComponentTypes;
using Hypernex.Game;
using Hypernex.Tools;

namespace Hypernex.CCK.GodotVersion
{
    public sealed class EntityScene
    {
        public string Version;
        public Entity RootEntity;
        public EntityAsset[] Assets;
    }

    public sealed class Entity
    {
        public string Name;
        public bool Enabled;
        public Entity[] Children;
        public Component[] Components;
    }

    public sealed class Component
    {
        public string TypeName;
        public ComponentData Data;

        public Component()
        {
        }

        public Component(ComponentData data)
        {
            TypeName = data.GetType().Name;
            Data = data;
        }
    }

    public sealed class EntityAsset
    {
        public string TypeName;
        public EntityAssetData Data;

        public EntityAsset()
        {
        }

        public EntityAsset(EntityAssetData data)
        {
            TypeName = data.GetType().Name;
            Data = data;
        }
    }

    public abstract class EntityAssetData
    {
        public string Path;
    }

    public abstract class ComponentData
    {
    }

    public partial class EntitySceneLoader : ISceneProvider
    {
        public void Dispose()
        {
        }

        public PackedScene LoadFromFile(string filePath)
        {
            return null;
        }

        public void SaveToFile(string filePath, PackedScene scene)
        {
            Node root = scene.Instantiate();
            if (root is IEntity ent)
            {
                Dictionary<Resource, EntityAsset> assets = new Dictionary<Resource, EntityAsset>();
                Entity entStruct = ToEntityStruct(ent, assets);
            }
            root.Free();
        }

        private Entity ToEntityStruct(IEntity ent, Dictionary<Resource, EntityAsset> assets)
        {
            Entity final = new Entity();
            final.Name = ent.Name;
            final.Enabled = ent.Enabled;
            Node[] cmps = ent.GetComponents<Node>();
            List<Component> components = new List<Component>();
            for (int i = 0; i < cmps.Length; i++)
            {
                if (cmps[i] is MeshInstance3D mi)
                {
                    assets.TryAdd(mi.Mesh, new EntityAsset(new MeshStruct(mi.Mesh)));
                    components.Add(new Component(new MeshComponent(mi)));
                }
            }
            final.Components = components.ToArray();
            IEntity[] ents = ent.GetChildEnts();
            final.Children = new Entity[ents.Length];
            for (int i = 0; i < ents.Length; i++)
            {
                final.Children[i] = ToEntityStruct(ents[i], assets);
            }
            return final;
        }
    }
}
