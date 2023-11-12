using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using Newtonsoft.Json;

namespace Hypernex.Game.Classes
{
    public partial class AssetCollider : WorldAsset
    {
        [JsonProperty]
        [Export]
        public Vector3[] Position { get; set; } = Array.Empty<Vector3>();

        [JsonProperty]
        public float[][] FPosition
        {
            get => Position.Select(x => x.ToFloats()).ToArray();
            set => Position = value.Select(x => x.ToGodot3()).ToArray();
        }

        public ConcavePolygonShape3D ToShape3D()
        {
            ConcavePolygonShape3D mesh = new ConcavePolygonShape3D();
            mesh.Data = Position.ToArray();
            return mesh;
        }

        public AssetCollider FromShape(Shape3D shape)
        {
            Name = shape.ResourceName;
            Path = shape.ResourcePath;
            if (shape is ConcavePolygonShape3D mesh)
            {
                Position = mesh.Data;
            }
            else
            {
                return null;
            }
            return this;
        }
    }
}
