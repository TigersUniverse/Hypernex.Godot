using System;
using System.Text;
using Godot;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using Newtonsoft.Json;

namespace Hypernex.Game.Classes
{
    [GlobalClass]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class WorldDescriptor : Node3D
    {
        [JsonProperty]
        [Export]
        public string Data { get; set; }

        [JsonProperty(nameof(StartPosition))]
        public float[] JsonStartPosition
        {
            get => StartPosition.ToFloats();
            set => StartPosition = value.ToGodot3();
        }

        [Export]
        public Vector3 StartPosition { get; set; }

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
