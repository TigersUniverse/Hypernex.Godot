using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using Newtonsoft.Json;

namespace Hypernex.Game.Classes
{
    [JsonObject(MemberSerialization.OptIn)]
    public partial class WorldAsset
    {
        [JsonProperty]
        public string Name { get; set; }
        [JsonProperty]
        public string Path { get; set; }
    }
}
