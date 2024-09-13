using System;
using Godot;
using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools.Godot;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Item
    {
        internal Node t;
        internal Node world;

        public Item()
        {
            throw new Exception("Item cannot be created by a Script!");
        }

        internal Item(Node t, Node root)
        {
            if (!GodotObject.IsInstanceValid(t))
                this.t = null;
            else if (root.IsAncestorOf(t))
                this.t = t;
            else
                this.t = root;
            world = root;
        }

        public string Name => t.Name;

        public bool Enabled
        {
            get
            {
                if (t is IEntity ent)
                    return ent.Enabled;
                return t.CanProcess();
            }
            set
            {
                if (t is IEntity ent)
                {
                    ent.Enabled = value;
                    return;
                }
                if (t is Node3D n3d)
                {
                    n3d.Visible = value;
                }
                else if (t is CanvasItem n2d)
                {
                    n2d.Visible = value;
                }
                t.ProcessMode = value ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
            }
        }

        public string Path => world.GetPathTo(t);

        public Item Parent
        {
            get => new Item(t.GetParent(), world);
            set
            {
                if (value == null)
                {
                    t.Reparent(world);
                }
                else if (value != null && world == value.world && GodotObject.IsInstanceValid(value.t))
                {
                    t.Reparent(value.t);
                }
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

        public int ChildCount => t.GetChildCount();

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
            Node tr = t.GetChild(i);
            return new Item(tr, world);
        }

        public Item GetChildByName(string name)
        {
            Node tr = t.FindChild(name, false);
            return new Item(tr, world);
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