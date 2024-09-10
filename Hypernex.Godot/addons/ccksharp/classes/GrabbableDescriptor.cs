using System;
using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    public partial class GrabbableDescriptor : Node, ISandboxClass
    {
        public const string TypeName = "GrabbableDescriptor";

        [Export]
        public bool ApplyVelocity = true;
        [Export]
        public float VelocityAmount = 10f;
        [Export]
        public float VelocityThreshold = 0.05f;
        [Export]
        public bool GrabByLaser = true;
        [Export]
        public float LaserGrabDistance = 5f;
        [Export]
        public bool GrabByDistance = true;
        [Export]
        public float GrabDistance = 3f;

        public CollisionObject3D parent;

        public override void _EnterTree()
        {
            parent = GetParent<CollisionObject3D>();
            parent.SetMeta(TypeName, this);
        }

        public override void _ExitTree()
        {
            if (IsInstanceValid(parent))
                parent.RemoveMeta(TypeName);
            parent = null;
        }
    }
}
