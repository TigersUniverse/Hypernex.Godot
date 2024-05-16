using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using Hypernex.Tools.Godot;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Item
    {
        internal Node t;

        public Item()
        {
            throw new Exception("Item cannot be created by a Script!");
        }

        internal Item(Node t) => this.t = t;

        public string Name => t.Name;

        public bool Enabled
        {
            get => t.IsProcessing() && t.IsPhysicsProcessing();
            set
            {
                t.SetProcess(value);
                t.SetPhysicsProcess(value);
            }
        }

        public string Path => t.GetPath();

        public Item Parent
        {
            get => new (t.GetParent());
            set
            {
                throw new NotImplementedException();
                /*
                Transform root = AnimationUtility.GetRootOfChild(t);
                if (root.GetComponent<LocalPlayer>() != null || root.GetComponent<NetPlayer>() != null)
                    return;
                t.parent = value.t;
                */
            }
        }
        
        public float3 Position
        {
            get => t is Node3D n3d ? n3d.GlobalPosition.ToFloat3() : Vector3.Zero.ToFloat3();
            set
            {
                if (t is Node3D n3d)
                    n3d.GlobalPosition = value.ToGodot3();
            }
        }

        public float4 Rotation
        {
            get => t is Node3D n3d ? n3d.GlobalBasis.GetRotationQuaternion().ToFloat4() : Quaternion.Identity.ToFloat4();
            set
            {
                if (t is Node3D n3d)
                    n3d.GlobalBasis = new Basis(value.ToGodotQuat());
            }
        }

        public float3 LocalPosition
        {
            get => t is Node3D n3d ? n3d.Position.ToFloat3() : Vector3.Zero.ToFloat3();
            set
            {
                if (t is Node3D n3d)
                    n3d.Position = value.ToGodot3();
            }
        }
        
        public float4 LocalRotation
        {
            get => t is Node3D n3d ? n3d.Basis.GetRotationQuaternion().ToFloat4() : Quaternion.Identity.ToFloat4();
            set
            {
                if (t is Node3D n3d)
                    n3d.Basis = new Basis(value.ToGodotQuat());
            }
        }

        public float3 LocalSize
        {
            get => t is Node3D n3d ? n3d.Scale.ToFloat3() : Vector3.Zero.ToFloat3();
            set
            {
                if (t is Node3D n3d)
                    n3d.Scale = value.ToGodot3();
            }
        }

        public int ChildCount => throw new NotImplementedException();

        public Item[] Children
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool CanCollide
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        // public Collider Collider => throw new NotImplementedException();
        // public Collider[] Colliders => throw new NotImplementedException();

        public Item GetChildByIndex(int i)
        {
            throw new NotImplementedException();
            /*
            Node tr = t.GetChild(i);
            if (tr != null)
            {
                Item item = new Item(tr);
                if (LocalLocalAvatar.IsAvatarItem(item) || LocalNetAvatar.IsAvatarItem(item))
                    return null;
                return item;
            }
            return null;
            */
        }

        public Item GetChildByName(string name)
        {
            throw new NotImplementedException();
            /*
            Transform tr = t.Find(name);
            if (tr != null)
            {
                Item item = new Item(tr);
                if (LocalLocalAvatar.IsAvatarItem(item) || LocalNetAvatar.IsAvatarItem(item))
                    return null;
                return item;
            }
            return null;
            */
        }
        
        public static bool operator ==(Item x, Item y) => x?.Equals(y) ?? false;
        public static bool operator !=(Item x, Item y) => !(x == y);
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(Item))
                return false;
            return t == ((Item) obj).t;
        }

        public override int GetHashCode()
        {
            return t?.GetHashCode() ?? default;
        }
    }
}