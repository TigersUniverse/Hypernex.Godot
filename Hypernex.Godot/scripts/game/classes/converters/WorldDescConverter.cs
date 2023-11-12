using System;
using Godot;
using Hypernex.Tools;
using Newtonsoft.Json.Linq;

namespace Hypernex.Game.Classes
{
    [ComponentConverterName("WorldDesc")]
    public partial class WorldDescConverter : WorldComponentConverter
    {
        public override bool CanHandleType(Type t)
        {
            return t == typeof(WorldDescriptor);
        }

        public override Node LoadFromData(WorldData root, JObject data)
        {
            return JsonTools.DeserializeObject<WorldDescriptor>(data);
        }

        public override JObject SaveToData(WorldData root, Node node)
        {
            return JsonTools.SerializeObject((WorldDescriptor)node);
        }
    }
}
