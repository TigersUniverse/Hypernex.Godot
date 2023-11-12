using System;
using System.Collections;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hypernex.Game.Classes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AssetConverterNameAttribute : Attribute
    {
        public readonly string Name;

        public AssetConverterNameAttribute(string name)
        {
            Name = name;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class WorldAssetConverter
    {
        public WorldAssetConverter()
        {
        }

        public abstract bool CanHandleType(Type t);

        public abstract WorldAsset LoadFromData(WorldData root, JObject data);

        public abstract JObject SaveToData(WorldData root, WorldAsset node);

        public virtual void PostLoad(WorldData root)
        {
        }

        public virtual void PreSave(WorldData root)
        {
        }
    }
}
