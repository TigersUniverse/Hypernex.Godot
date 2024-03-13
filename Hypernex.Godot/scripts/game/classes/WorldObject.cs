using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hypernex.Game.Classes
{
    [GlobalClass]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class WorldObject : Resource
    {
        public WorldRoot World { get; set; } = null;
        public Node Root { get; private set; } = null;
        public List<Node> Components { get; private set; } = new List<Node>();
        private string _name = string.Empty;
        private Transform3D _xform = Transform3D.Identity;
        internal int _parent = -1;

        [JsonProperty("Name")]
        public string Name
        {
            get
            {
                if (GodotObject.IsInstanceValid(Root))
                    return Root.Name;
                return default;
            }
            set
            {
                if (GodotObject.IsInstanceValid(Root))
                    Root.Name = value;
                _name = value;
            }
        }

        [JsonProperty("Transform")]
        public float[] JsonTransform
        {
            get => Transform.ToFloats();
            set => Transform = value.ToGodot3D();
        }

        public Transform3D Transform
        {
            get
            {
                if (GodotObject.IsInstanceValid(Root) && Root is Node3D n)
                    return n.Transform;
                return default;
            }
            set
            {
                if (GodotObject.IsInstanceValid(Root) && Root is Node3D n)
                    n.Transform = value;
                _xform = value;
            }
        }

        public WorldObject GetParent()
        {
            return World.Objects[World.GetParentObjectIndex(this)];
        }

        public int GetParentIndex()
        {
            return World.GetParentObjectIndex(this);
        }

        public T GetComponent<T>() where T : Node
        {
            return (T)Components.FirstOrDefault(x => x is T);
        }

        public T[] GetComponents<T>() where T : Node
        {
            return Components.Where(x => x is T).Select(x => (T)x).ToArray();
        }

        public static bool IsComponentRoot(Node node)
        {
            return true;
        }

        public Node FinalizeComponents()
        {
            if (GodotObject.IsInstanceValid(Root))
                return Root;
            foreach (var cmp in Components)
            {
                if (IsComponentRoot(cmp))
                    Root = cmp;
            }
            if (!GodotObject.IsInstanceValid(Root))
                Root = new Node3D();
            Name = _name;
            Transform = _xform;
            foreach (var cmp in Components)
            {
                if (Root == cmp)
                    continue;
                Root.AddChild(cmp);
                cmp.SetMeta(nameof(WorldObject), Variant.From(this));
            }
            return Root;
        }

        public void LoadFromData(JObject data)
        {
            JsonTools.PopulateObject(data, this);
        }

        public JObject SaveToData()
        {
            return JsonTools.SerializeObject(this);
        }

        public static JObject SaveToData(Node3D node)
        {
            JObject obj = new JObject();
            obj["Name"] = JToken.FromObject(node.Name.ToString());
            obj["Transform"] = JToken.FromObject(node.Transform.ToFloats());
            return obj;
        }
    }
}