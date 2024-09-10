using System;
using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    public partial class RespawnableDescriptor : Node, ISandboxClass
    {
        public const string TypeName = "RespawnableDescriptor";

        [Export]
        public float LowestPointRespawnThreshold = 50f;

        public Node3D parent;
        public Transform3D StartPosition;

        public override void _EnterTree()
        {
            parent = GetParentOrNull<Node3D>();
            parent.SetMeta(TypeName, this);
        }

        public override void _Ready()
        {
            StartPosition = parent.GlobalTransform;
        }

        public override void _ExitTree()
        {
            if (IsInstanceValid(parent))
                parent.RemoveMeta(TypeName);
            parent = null;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (parent.GlobalPosition.Y < -LowestPointRespawnThreshold)
            {
                parent.GlobalTransform = StartPosition;
                if (parent is RigidBody3D rb)
                {
                    rb.LinearVelocity = Vector3.Zero;
                    rb.AngularVelocity = Vector3.Zero;
                }
            }
        }
    }
}
