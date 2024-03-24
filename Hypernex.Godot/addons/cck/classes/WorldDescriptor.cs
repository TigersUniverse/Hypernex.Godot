using System;
using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    [GlobalClass]
    public partial class WorldDescriptor : Node3D
    {
        [Export]
        public string Data { get; set; }

        [Export]
        public Vector3 StartPosition { get; set; }
    }
}
