using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using Newtonsoft.Json;

namespace Hypernex.Game.Classes
{
    [GlobalClass]
    public partial class WorldStaticBody : StaticBody3D, IWorldClass
    {
        public static string ClassName => "StaticBody";

        [JsonProperty]
        public float[] JsonTransform { get; set; }

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
            Transform = JsonTransform.ToGodot3D();
        }

        public void PreSave()
        {
            JsonTransform = Transform.ToFloats();
        }
    }
}
