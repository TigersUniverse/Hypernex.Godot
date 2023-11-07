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
    [GlobalClass]
    public partial class AssetMesh : Resource, IWorldClass
    {
        public static string ClassName => "AssetMesh";

        [JsonProperty]
        [Export]
        public int[] Index { get; set; } = Array.Empty<int>();
        [Export]
        public Vector3[] Position { get; set; } = Array.Empty<Vector3>();
        [Export]
        public Vector3[] Normal { get; set; } = Array.Empty<Vector3>();
        public Vector4[] Tangent { get; set; } = Array.Empty<Vector4>();
        [Export]
        public Vector2[] UV0 { get; set; } = Array.Empty<Vector2>();
        [Export]
        public Vector2[] UV1 { get; set; } = Array.Empty<Vector2>();

        [JsonProperty]
        public float[][] FPosition
        {
            get => Position.Select(x => x.ToFloats()).ToArray();
            set => Position = value.Select(x => x.ToGodot3()).ToArray();
        }

        [JsonProperty]
        public float[][] FNormal
        {
            get => Normal.Select(x => x.ToFloats()).ToArray();
            set => Normal = value.Select(x => x.ToGodot3()).ToArray();
        }

        [JsonProperty]
        public float[][] FTangent
        {
            get => Tangent.Select(x => x.ToFloats()).ToArray();
            set => Tangent = value.Select(x => x.ToGodot4()).ToArray();
        }

        [JsonProperty]
        public float[][] FUV0
        {
            get => UV0.Select(x => x.ToFloats()).ToArray();
            set => UV0 = value.Select(x => x.ToGodot2()).ToArray();
        }

        [JsonProperty]
        public float[][] FUV1
        {
            get => UV1.Select(x => x.ToFloats()).ToArray();
            set => UV1 = value.Select(x => x.ToGodot2()).ToArray();
        }

        public void LoadFromData(string data)
        {
            JsonTools.JsonPopulate(data, this);
        }

        public string SaveToData()
        {
            return JsonTools.JsonSerialize(this);
        }
    }
}
