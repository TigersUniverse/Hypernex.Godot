using System;
using System.Collections;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hypernex.Game.Classes
{
    public class ComponentConverterNameAttribute : Attribute
    {
        public readonly string Name;

        public ComponentConverterNameAttribute(string name)
        {
            Name = name;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class WorldComponentConverter
    {
        public WorldComponentConverter()
        {
        }

        public abstract bool CanHandleType(Type t);

        public abstract Node LoadFromData(WorldData root, JObject data);

        public abstract JObject SaveToData(WorldData root, Node node);

        public virtual void PostLoad(WorldData root)
        {
        }

        public virtual void PreSave(WorldData root)
        {
        }
    }
}
