using System;
using System.Text;
using Godot;
using Newtonsoft.Json;

namespace Hypernex.Game.Classes
{
    public partial class WorldDescriptor : Node3D, IWorldClass
    {
        public static string ClassName => "World";

        [JsonProperty]
        [Export]
        public string Data { get; set; }

        public void LoadFromData(string data)
        {
            JsonConvert.PopulateObject(Encoding.UTF8.GetString(Convert.FromBase64String(data)), this);
        }

        public string SaveToData()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this)));
        }
    }
}
