using System;
using Godot;
using Hypernex.Tools;
using Newtonsoft.Json;

namespace Hypernex.Game.Classes
{
    [JsonObject(MemberSerialization.OptIn)]
    public partial class WorldComponent
    {
        public const string ClassNameName = "ClassName";

        public WorldObject WorldObject { get; internal set; }
        public Node3D Value { get; internal set; }

        public WorldComponent()
        {
        }

        public virtual void LoadFromData(string data)
        {
            JsonTools.JsonPopulate(data, this);
        }

        public virtual string SaveToData()
        {
            return JsonTools.JsonSerialize(this);
        }

        public virtual void PostLoad()
        {
        }

        public virtual void PreSave()
        {
        }
    }

    public partial class WorldComponent<T> : WorldComponent where T : Node3D
    {
        public new T Value { get => (T)base.Value; internal set => base.Value = value; }

        public WorldComponent() : base()
        {
        }
    }
}