using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hypernex.Game.Classes
{
    [ComponentConverterName("StaticBody")]
    public partial class StaticBodyConverter : WorldComponentConverter
    {
        public class Data
        {
            // public float[] Transform { get; set; }
        }

        public override bool CanHandleType(Type t)
        {
            return t == typeof(StaticBody3D);
        }

        public override Node LoadFromData(WorldData root, JObject data)
        {
            var d = JsonTools.DeserializeObject<Data>(data);
            var n = new StaticBody3D();
            // n.Transform = d.Transform.ToGodot3D();
            return n;
        }

        public override JObject SaveToData(WorldData root, Node node)
        {
            var n = (StaticBody3D)node;
            var d = new Data();
            // d.Transform = n.Transform.ToFloats();
            return JsonTools.SerializeObject(d);
        }
    }
}
