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
    public partial class AssetCollider : Resource, IWorldClass
    {
        public static string ClassName => "AssetCollider";

        [JsonProperty]
        [Export]
        public Vector3[] Position { get; set; } = Array.Empty<Vector3>();

        [JsonProperty]
        public float[][] FPosition
        {
            get => Position.Select(x => x.ToFloats()).ToArray();
            set => Position = value.Select(x => x.ToGodot3()).ToArray();
        }

        public void LoadFromData(string data)
        {
            JsonTools.JsonPopulate(data, this);
        }

        public string SaveToData()
        {
            return JsonTools.JsonSerialize(this);
        }

        public void PostLoad()
        {
        }

        public void PreSave()
        {
        }
    }
}
