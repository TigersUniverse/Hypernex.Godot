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
                    Script script = null;
                    for (int j = 0; j < nodePropCount[i]; j++)
                    {
                        // GD.Print(nodePropValues[i][j].data.GetType());
                        if (nodePropNames[i][j].Equals("script", StringComparison.OrdinalIgnoreCase))
                        {
                            script = (Script)nodePropValues[i][j].GetData(convertDb);
                            // node.SetScript(nodePropValues[i][j].GetData(convertDb));
                        }
                    }
                    Node node = script is CSharpScript cs ? cs.New().As<Node>() : ClassDB.Instantiate(type).As<Node>();
                    node.Name = GetName(path);
                    for (int j = 0; j < nodePropCount[i]; j++)
                    {
                        if (!nodePropNames[i][j].Equals("script", StringComparison.OrdinalIgnoreCase))
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
            root.Free();
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
