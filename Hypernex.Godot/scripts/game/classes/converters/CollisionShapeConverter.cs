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
    [ComponentConverterName("CollisionShape")]
    public partial class CollisionShapeConverter : WorldComponentConverter
    {
        public class Data
        {
            public float[] Transform { get; set; }
            public string ShapePath { get; set; }
        }

        public override bool CanHandleType(Type t)
        {
            return t == typeof(CollisionShape3D);
        }

        public override Node LoadFromData(WorldData root, JObject data)
        {
            var d = JsonTools.DeserializeObject<Data>(data);
            var n = new CollisionShape3D();
            n.Transform = d.Transform.ToGodot3D();
            n.Shape = root.LoadAssetFromPath<AssetCollider>(d.ShapePath).ToShape3D();
            return n;
        }

        public override JObject SaveToData(WorldData root, Node node)
        {
            var n = (CollisionShape3D)node;
            var d = new Data();
            d.Transform = n.Transform.ToFloats();
            d.ShapePath = root.SaveAssetFromResource(new AssetCollider().FromShape(n.Shape));
            return JsonTools.SerializeObject(d);
        }
    }
}
