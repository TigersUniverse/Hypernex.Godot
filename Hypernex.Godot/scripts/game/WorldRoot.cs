using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Game.Classes;
using Hypernex.Tools;

namespace Hypernex.Game
{
    public partial class WorldRoot : Node
    {
        public GameInstance gameInstance;
        public WorldDescriptor descriptor;
        public List<WorldObject> Objects = new List<WorldObject>();
        public List<Node> Components = new List<Node>();
        public List<WorldAsset> Assets = new List<WorldAsset>();

        public int GetParentObjectIndex(WorldObject worldObject)
        {
            if (IsInstanceValid(worldObject.Root))
            {
                var par = worldObject.Root.GetParent();
                return Objects.FindIndex(x => x.Root == par);
            }
            return -1;
        }

        public void AddPlayer(PlayerRoot player)
        {
            player.Position = descriptor.StartPosition;
            AddChild(player);
        }

        public void AddObject(WorldObject worldObject)
        {
            worldObject.World = this;
            Objects.Add(worldObject);
        }

        public void AddComponent(Node worldObject)
        {
            if (worldObject is WorldDescriptor desc)
                descriptor = desc;
            Components.Add(worldObject);
        }

        public void AddAsset(WorldAsset worldObject)
        {
            Assets.Add(worldObject);
        }

        private void Load(int parent)
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                var obj = Objects[i];
                if (obj._parent != parent)
                    continue;
                if (parent == -1)
                    AddChild(obj.FinalizeComponents());
                else
                    Objects[parent].Root.AddChild(obj.FinalizeComponents());
                Load(i);
            }
        }

        public void Load()
        {
            Load(-1);
        }
    }
}
