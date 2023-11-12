using System;
using System.Text;
using Godot;
using Hypernex.Tools;
using Newtonsoft.Json;

namespace Hypernex.Game.Classes
{
    [GlobalClass]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class WorldDescriptor : Node
    {
        public static string ClassName => "World";

        [JsonProperty]
        [Export]
        public string Data { get; set; }

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
