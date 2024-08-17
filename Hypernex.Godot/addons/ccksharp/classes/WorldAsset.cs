using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace Hypernex.CCK.GodotVersion.Classes
{
    public partial class WorldAsset : Resource, ISandboxClass
    {
        public const string TypeName = "WorldAsset";

        [Export]
        public string name { get; set; }
        [Export]
        public Resource asset { get; set; }
    }
}
