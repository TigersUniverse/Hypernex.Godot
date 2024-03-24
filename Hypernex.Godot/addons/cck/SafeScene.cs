using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.CCK.GodotVersion.Converters;
using Hypernex.Tools;
using Newtonsoft.Json.Linq;

namespace Hypernex.CCK.GodotVersion
{
    public partial class SafeScene
    {
        public static bool IsParentPath(NodePath path, NodePath parent)
        {
            if (parent.GetNameCount() != path.GetNameCount() - 1)
            {
                return false;
            }
            for (int i = 0; i < path.GetNameCount() - 1; i++)
            {
                if (path.GetName(i) != parent.GetName(i))
                {
                    return false;
                }
            }
            return true;
        }

        public static NodePath GetParent(NodePath path)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < path.GetNameCount() - 1; i++)
            {
                names.Add(path.GetName(i));
            }
            return string.Join('/', names);
        }

        public static string GetName(NodePath path)
        {
            return path.GetName(path.GetNameCount() - 1);
        }

        public static bool IsRoot(NodePath path)
        {
            int j = 0;
            for (int i = 0; i < path.GetNameCount(); i++)
            {
                if (path.GetName(i) != ".")
                {
                    j++;
                }
            }
            return j == 0;
        }

        public partial class SafeObject
        {
            public int vType;
            public string oType;
            public object data;

            public SafeObject()
            {
            }

            public SafeObject(Variant v, ConvertDB convertDb)
            {
                vType = (int)v.VariantType;
                if (v.VariantType == Variant.Type.Object)
                {
                    if (v.Obj == null)
                    {
                        oType = null;
                        data = null;
                        return;
                    }
                    oType = v.AsGodotObject().GetClass();
                    data = convertDb.ConvertObject(oType, v.AsGodotObject());
                }
                else
                {
                    data = v.Obj;
                    switch (v.VariantType)
                    {
                        case Variant.Type.PackedByteArray:
                            data = v.AsByteArray();
                            break;
                    }
                }
            }

            public Variant GetData(ConvertDB convertDb)
            {
                if (vType == (int)Variant.Type.Object)
                {
                    if (!string.IsNullOrEmpty(oType) && convertDb.CanConvert(oType))
                    {
                        return convertDb.Convert(oType, data);
                    }
                    return default;
                }
                switch ((Variant.Type)vType)
                {
                    default:
                        return default;
                    case Variant.Type.Bool:
                        return Variant.From((bool)data);
                    case Variant.Type.Int:
                        return Variant.From((int)(long)data);
                    case Variant.Type.Float:
                        return Variant.From((float)(double)data);
                    case Variant.Type.String:
                        return Variant.From((string)data);
                    case Variant.Type.Vector2:
                        return Variant.From(((JObject)data).ToObject<Vector2>());
                    case Variant.Type.Vector2I:
                        return Variant.From(((JObject)data).ToObject<Vector2I>());
                    case Variant.Type.Rect2:
                        return Variant.From(((JObject)data).ToObject<Rect2>());
                    case Variant.Type.Rect2I:
                        return Variant.From(((JObject)data).ToObject<Rect2I>());
                    case Variant.Type.Vector3:
                        return Variant.From(((JObject)data).ToObject<Vector3>());
                    case Variant.Type.Vector3I:
                        return Variant.From(((JObject)data).ToObject<Vector3I>());
                    case Variant.Type.Transform2D:
                        return Variant.From(((JObject)data).ToObject<Transform2D>());
                    case Variant.Type.Vector4:
                        return Variant.From(((JObject)data).ToObject<Vector4>());
                    case Variant.Type.Vector4I:
                        return Variant.From(((JObject)data).ToObject<Vector4I>());
                    case Variant.Type.Plane:
                        return Variant.From(((JObject)data).ToObject<Plane>());
                    case Variant.Type.Quaternion:
                        return Variant.From(((JObject)data).ToObject<Quaternion>());
                    case Variant.Type.Aabb:
                        return Variant.From(((JObject)data).ToObject<Aabb>());
                    case Variant.Type.Basis:
                        return Variant.From(((JObject)data).ToObject<Basis>());
                    case Variant.Type.Transform3D:
                        return Variant.From(((JObject)data).ToObject<Transform3D>());
                    case Variant.Type.Projection:
                        return Variant.From(((JObject)data).ToObject<Projection>());
                    case Variant.Type.Color:
                        return Variant.From(((JObject)data).ToObject<Color>());
                    case Variant.Type.StringName:
                        return Variant.From(((JObject)data).ToObject<StringName>());
                    case Variant.Type.NodePath:
                        return Variant.From(((JObject)data).ToObject<NodePath>());
                    case Variant.Type.Rid:
                        return Variant.From(((JObject)data).ToObject<Rid>());
                    case Variant.Type.Callable:
                        return default;
                    case Variant.Type.Signal:
                        return default;
                    case Variant.Type.Dictionary:
                        return Variant.From(((JObject)data).ToObject<Godot.Collections.Dictionary>());
                    case Variant.Type.Array:
                        return Variant.From(((JArray)data).ToObject<Godot.Collections.Array>());
                    case Variant.Type.PackedByteArray:
                        return Variant.From(((JArray)data).ToObject<byte[]>());
                    case Variant.Type.PackedInt32Array:
                        return Variant.From(((JArray)data).ToObject<int[]>());
                    case Variant.Type.PackedInt64Array:
                        return Variant.From(((JArray)data).ToObject<long[]>());
                    case Variant.Type.PackedFloat32Array:
                        return Variant.From(((JArray)data).ToObject<float[]>());
                    case Variant.Type.PackedFloat64Array:
                        return Variant.From(((JArray)data).ToObject<double[]>());
                    case Variant.Type.PackedStringArray:
                        return Variant.From(((JArray)data).ToObject<string[]>());
                    case Variant.Type.PackedVector2Array:
                        return Variant.From(((JArray)data).ToObject<Vector2[]>());
                    case Variant.Type.PackedVector3Array:
                        return Variant.From(((JArray)data).ToObject<Vector3[]>());
                    case Variant.Type.PackedColorArray:
                        return Variant.From(((JArray)data).ToObject<Color[]>());
                }
            }
        }

        public int nodeCount = 0;
        public List<string> nodeTypes = new List<string>();
        public List<string> nodePaths = new List<string>();
        public List<int> nodePropCount = new List<int>();
        public List<List<string>> nodePropNames = new List<List<string>>();
        public List<List<SafeObject>> nodePropValues = new List<List<SafeObject>>();

        public void SetupFromState(SceneState state, ConvertDB convertDb)
        {
            nodeCount = state.GetNodeCount();
            nodeTypes.Clear();
            nodePaths.Clear();
            nodePropCount.Clear();
            nodePropNames.Clear();
            nodePropValues.Clear();
            for (int i = 0; i < state.GetNodeCount(); i++)
            {
                nodeTypes.Add(state.GetNodeType(i));
                nodePaths.Add(state.GetNodePath(i));
                nodePropNames.Add(new List<string>());
                nodePropValues.Add(new List<SafeObject>());
                for (int j = 0; j < state.GetNodePropertyCount(i); j++)
                {
                    Variant val = state.GetNodePropertyValue(i, j);
                    nodePropValues[i].Add(new SafeObject(val, convertDb));
                    nodePropNames[i].Add(state.GetNodePropertyName(i, j));
                }
                nodePropCount.Add(nodePropNames[i].Count);
            }
        }

        public PackedScene SetupToPackedScene(ConvertDB convertDb)
        {
            Dictionary<NodePath, Node> nodes = new Dictionary<NodePath, Node>();
            for (int i = 0; i < nodeCount; i++)
            {
                NodePath path = nodePaths[i];
                StringName type = nodeTypes[i];
                if (ClassDB.CanInstantiate(type) && ClassDB.IsParentClass(type, nameof(Node)))
                {
                    Node node = ClassDB.Instantiate(type).As<Node>();
                    node.Name = GetName(path);
                    for (int j = 0; j < nodePropCount[i]; j++)
                    {
                        // GD.Print(nodePropValues[i][j].data.GetType());
                        node.Set(nodePropNames[i][j], nodePropValues[i][j].GetData(convertDb));
                    }
                    nodes.Add(path, node);
                }
            }
            Node root = nodes.FirstOrDefault(x => IsRoot(x.Key)).Value;
            Node lastParent = root;
            NodePath lastPath = ".";
            List<NodePath> paths = new List<NodePath>(nodes.Keys);
            int k = 0;
            while (paths.Count > 0 && k < 20)
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    Node parent = root.GetNodeOrNull(GetParent(paths[i]));
                    if (GodotObject.IsInstanceValid(parent))
                    {
                        parent.AddChild(nodes[paths[i]]);
                        nodes[paths[i]].Owner = root;
                        paths.RemoveAt(i);
                        i--;
                    }
                }
                k++;
            }
            PackedScene safeScn = new PackedScene();
            safeScn.Pack(root);
            return safeScn;
        }

        public void FromString(string str)
        {
            JsonTools.JsonPopulate(str, this);
        }

        public override string ToString()
        {
            return JsonTools.JsonSerialize(this);
        }
    }
}
