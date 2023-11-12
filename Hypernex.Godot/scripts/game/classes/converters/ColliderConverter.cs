using System;
using Hypernex.Tools;
using Newtonsoft.Json.Linq;

namespace Hypernex.Game.Classes
{
    [AssetConverterName("Collider")]
    public partial class ColliderConverter : WorldAssetConverter
    {
        public override bool CanHandleType(Type t)
        {
            return t == typeof(AssetCollider);
        }

        public override WorldAsset LoadFromData(WorldData root, JObject data)
        {
            return JsonTools.DeserializeObject<AssetCollider>(data);
        }

        public override JObject SaveToData(WorldData root, WorldAsset node)
        {
            return JsonTools.SerializeObject((AssetCollider)node);
        }
    }
}
