using System;
using Hypernex.Tools;
using Newtonsoft.Json.Linq;

namespace Hypernex.Game.Classes
{
    [AssetConverterName("Mesh")]
    public partial class MeshConverter : WorldAssetConverter
    {
        public override bool CanHandleType(Type t)
        {
            return t == typeof(AssetMesh);
        }

        public override WorldAsset LoadFromData(WorldData root, JObject data)
        {
            return JsonTools.DeserializeObject<AssetMesh>(data);
        }

        public override JObject SaveToData(WorldData root, WorldAsset node)
        {
            return JsonTools.SerializeObject((AssetMesh)node);
        }
    }
}
