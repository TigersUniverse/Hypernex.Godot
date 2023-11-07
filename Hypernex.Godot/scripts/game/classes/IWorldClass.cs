using System.Collections;
using Newtonsoft.Json;

namespace Hypernex.Game.Classes
{
    [JsonObject(MemberSerialization.OptIn)]
    public interface IWorldClass
    {
        public const string ClassNameName = "ClassName";
        void LoadFromData(string data);
        string SaveToData();
    }
}
