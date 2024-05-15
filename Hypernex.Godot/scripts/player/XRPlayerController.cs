using System.Collections.Generic;
using Godot;
using Hypernex.Game;

namespace Hypernex.Player
{
    public partial class XRPlayerController : Node
    {
        [Export]
        public PlayerRoot root;
        [Export]
        public CharacterBody3D xrBody;
    }
}
