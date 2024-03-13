using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Hypernex.Game
{
    public class WorldData
    {
        public List<WorldDataObject> AllObjects { get; set; } = new List<WorldDataObject>();
        public List<WorldDataScript> Scripts { get; set; } = new List<WorldDataScript>();
        public List<WorldDataAsset> AllAssets { get; set; } = new List<WorldDataAsset>();
    }

    public class WorldDataObject
    {
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public JObject ClassData { get; set; } = null;
        public int ParentObject { get; set; } = -1;
        public List<WorldDataComponent> Components { get; set; } = new List<WorldDataComponent>();
    }

    public class WorldDataComponent
    {
        public string ClassName { get; set; } = string.Empty;
        public JObject ClassData { get; set; } = null;
    }

    public class WorldDataAsset
    {
        public string Path { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public JObject ClassData { get; set; } = null;
    }

    public class WorldDataScript
    {
        public string Name { get; set; } = string.Empty;
        public string Lang { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
