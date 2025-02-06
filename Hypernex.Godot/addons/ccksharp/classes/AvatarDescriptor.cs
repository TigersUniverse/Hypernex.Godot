using System;
using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    [Tool]
    [GlobalClass]
    public partial class AvatarDescriptor : Node3D, ISandboxClass
    {
        public const string TypeName = "AvatarDescriptor";

        [Export]
        public NodePath Skeleton { get; set; }
        [Export]
        public NodePath Eyes { get; set; }

        public Skeleton3D GetSkeleton() => GetNode<Skeleton3D>(Skeleton);

        public Node3D GetEyes() => GetNodeOrNull<Node3D>(Eyes);
    }
}
