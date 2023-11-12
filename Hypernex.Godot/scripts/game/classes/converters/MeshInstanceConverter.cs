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
    [ComponentConverterName("MeshInstance")]
    public partial class MeshInstanceConverter : WorldComponentConverter
    {
        public class Data
        {
            public float[] Transform { get; set; }
            public string MeshPath { get; set; }
        }

        public override bool CanHandleType(Type t)
        {
            return t == typeof(MeshInstance3D);
        }

        public override Node LoadFromData(WorldData root, JObject data)
        {
            var d = JsonTools.DeserializeObject<Data>(data);
            var n = new MeshInstance3D();
            n.Transform = d.Transform.ToGodot3D();
            n.Mesh = root.LoadAssetFromPath<AssetMesh>(d.MeshPath).ToMesh();
            return n;
        }

        public override JObject SaveToData(WorldData root, Node node)
        {
            var n = (MeshInstance3D)node;
            var d = new Data();
            d.Transform = n.Transform.ToFloats();
            d.MeshPath = root.SaveAssetFromResource(new AssetMesh().FromMesh(n.Mesh));
            return JsonTools.SerializeObject(d);
        }
    }
}
