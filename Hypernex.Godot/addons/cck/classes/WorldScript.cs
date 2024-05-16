using System;
using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    [GlobalClass]
    public partial class WorldScript : Node, ISandboxClass
    {
        [Export]
        public NexboxLanguage Language;
        [Export(PropertyHint.MultilineText)]
        public string Contents;
    }
}
