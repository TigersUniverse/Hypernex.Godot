using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Player;

namespace Hypernex.Game
{
    public partial class PlayerRoot : Node3D
    {
        [Export]
        public Node3D view;
        [Export]
        public NodePath[] parts = Array.Empty<NodePath>();
        public Node[] Parts => parts.Select(x => GetNode(x)).ToArray();

        public T GetPart<T>() where T : Node
        {
            return (T)Parts.FirstOrDefault(x => x is T);
        }
    }
}
