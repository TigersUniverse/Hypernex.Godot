using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace Hypernex.CCK.GodotVersion.Classes
{
    // [GlobalClass]
    public partial class WorldDescriptor : Node3D, ISandboxClass
    {
        public const string TypeName = "WorldDescriptor";

        [Export]
        public Vector3 StartPosition { get; set; }
        [Export]
        public NodePath[] StartPositions { get; set; }
        [Export]
        public Godot.Collections.Array<WorldAsset> Assets { get; set; }

        public Node3D GetRandomSpawn() => GetNode<Node3D>(StartPositions[GD.Randi() % StartPositions.Length]);

        public Resource GetAssetByName(string n) => Assets?.FirstOrDefault(x => x.GetMeta("name").AsString() == n);

        public WorldAsset[] GetWorldAssets()
        {
            return null;
            // Assets.Select(x => );
        }
    }
}
