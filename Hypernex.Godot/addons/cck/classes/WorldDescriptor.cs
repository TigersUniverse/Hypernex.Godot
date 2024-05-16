using System;
using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    [GlobalClass]
    public partial class WorldDescriptor : Node3D, ISandboxClass
    {
        [Export]
        public Vector3 StartPosition { get; set; }
    }
}
