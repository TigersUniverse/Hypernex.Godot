using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Networking.Messages.Data;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class ReadonlyItem
    {
        internal Item item;

        public ReadonlyItem()
        {
            throw new Exception("ReadonlyItem cannot be created by a Script!");
        }

        internal ReadonlyItem(Node t, Node root) => item = new Item(t, root);

        public string Name => item.Name;

        public bool Enabled => item.Enabled;
        public string Path => item.Path;
        public ReadonlyItem Parent => new(item.t.GetParent(), item.world);
        
        public float3 Position => item.Position;
        public float4 Rotation => item.Rotation;

        public float3 LocalPosition => item.LocalPosition;
        public float4 LocalRotation => item.LocalRotation;
        public float3 LocalSize => item.LocalSize;
        
        public int ChildCount => item.ChildCount;

        public ReadonlyItem[] Children
        {
            get
            {
                List<ReadonlyItem> items = new();
                foreach (Item itemChild in item.Children)
                    items.Add(new ReadonlyItem(itemChild.t, itemChild.world));
                return items.ToArray();
            }
        }
        
        public bool CanCollide => item.CanCollide;

        // public Collider Collider => item.Collider;
        // public Collider[] Colliders => item.Colliders;

        public ReadonlyItem GetChildByIndex(int i) => Children[i];

        public ReadonlyItem GetChildByName(string name) => Children.First(x => x.Name == name);

        public static bool operator ==(ReadonlyItem x, ReadonlyItem y) => x?.Equals(y) ?? false;
        public static bool operator !=(ReadonlyItem x, ReadonlyItem y) => !(x == y);
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(ReadonlyItem))
                return false;
            return item.t == ((ReadonlyItem) obj).item.t;
        }

        public override int GetHashCode()
        {
            return item.t?.GetHashCode() ?? default;
        }
    }
}