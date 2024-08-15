using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;
using Hypernex.CCK.GodotVersion.Classes;

namespace Hypernex.CCK.GodotVersion
{
    public partial class SafeLoader
    {
        public class ParsedSubTres
        {
            public string Type;
            public Dictionary<string, string> Properties = new Dictionary<string, string>();
        }

        public class ParsedTres
        {
            public string Type;
            public Dictionary<string, string> ExtResources = new Dictionary<string, string>();
            public Dictionary<string, string> Properties = new Dictionary<string, string>();

            public Dictionary<string, Resource> LoadedExtResources = new Dictionary<string, Resource>();

            public Variant ConvertPropertyString(string prop)
            {
                if (prop.Trim().StartsWith("ExtResource"))
                {
                    int startIdx = prop.Find("(\"");
                    int endIdx = prop.Find("\")", startIdx);
                    string id = prop.Substr(startIdx + 2, endIdx - startIdx - 2);
                    if (LoadedExtResources.ContainsKey(id))
                        return LoadedExtResources[id];
                    return new Variant();
                }
                if (prop.Trim().StartsWith("["))
                {
                    List<string> items = ParseArrayItems(prop.Trim());
                    Godot.Collections.Array arr = new Godot.Collections.Array();
                    foreach (var item in items)
                    {
                        arr.Add(ConvertPropertyString(item));
                    }
                    return arr;
                }
                if (prop.Trim().StartsWith("{"))
                {
                    Dictionary<string, string> items = ParseDictionaryItems(prop.Trim());
                    Godot.Collections.Dictionary dict = new Godot.Collections.Dictionary();
                    foreach (var item in items)
                    {
                        dict.Add(item.Key, ConvertPropertyString(item.Value));
                    }
                    return dict;
                }
                return SafeLoader.ConvertPropertyString(prop);
            }

            public Resource ToResource()
            {
                if (ClassDB.IsParentClass(Type, nameof(Script)))
                    return null;
                Resource res = ClassDB.Instantiate(Type).As<Resource>();
                foreach (var kvp in Properties)
                {
                    if (kvp.Key == Resource.PropertyName.ResourcePath)
                        continue;
                    res.Set(kvp.Key, ConvertPropertyString(kvp.Value));
                }
                return res;
            }
        }

        public class ParsedNode
        {
            public string Name;
            public string Parent;
            public string Instance;
            public string Type;
            public Dictionary<string, string> Properties = new Dictionary<string, string>();
        }

        public class ParsedConnection
        {
            public string Signal;
            public string From;
            public string To;
            public string Method;
        }

        public class ParsedTscn
        {
            public Dictionary<string, string> ExtResources = new Dictionary<string, string>();
            public Dictionary<string, ParsedSubTres> SubResources = new Dictionary<string, ParsedSubTres>();
            public List<ParsedNode> Nodes = new List<ParsedNode>();
            public List<ParsedConnection> Connections = new List<ParsedConnection>();

            public Dictionary<string, Resource> LoadedSubResources = new Dictionary<string, Resource>();
            public Dictionary<string, Resource> LoadedExtResources = new Dictionary<string, Resource>();

            public Variant ConvertPropertyString(string prop)
            {
                if (prop.Trim().StartsWith("ExtResource"))
                {
                    int startIdx = prop.Find("(");
                    int endIdx = prop.Find(")", startIdx);
                    int begin = prop[startIdx + 1] == '"' ? 2 : 1;
                    int end = prop[startIdx + 1] == '"' ? 3 : 1;
                    string id = prop.Substr(startIdx + begin, endIdx - startIdx - end);
                    if (LoadedExtResources.ContainsKey(id))
                        return LoadedExtResources[id];
                    // GD.Print($"Failed {id}");
                    return new Variant();
                }
                if (prop.Trim().StartsWith("SubResource"))
                {
                    int startIdx = prop.Find("(");
                    int endIdx = prop.Find(")", startIdx);
                    string id = prop.Substr(startIdx + 2, endIdx - startIdx - 3);

                    if (LoadedSubResources.ContainsKey(id))
                        return LoadedSubResources[id];

                    var sub = SubResources[id];
                    if (ClassDB.IsParentClass(sub.Type, nameof(Script)))
                        return new Variant();
                        // sub.Type = nameof(Resource);
                    Resource res = ClassDB.Instantiate(sub.Type).As<Resource>();
                    foreach (var kvp in sub.Properties)
                    {
                        if (kvp.Key == Resource.PropertyName.ResourcePath)
                            continue;
                        // GD.PrintS(kvp.Key, kvp.Value);
                        res.Set(kvp.Key, ConvertPropertyString(kvp.Value));
                    }
                    LoadedSubResources.TryAdd(id, res);
                    return res;
                }
                if (prop.Trim().StartsWith("["))
                {
                    List<string> items = ParseArrayItems(prop.Trim());
                    Godot.Collections.Array arr = new Godot.Collections.Array();
                    foreach (var item in items)
                    {
                        arr.Add(ConvertPropertyString(item));
                    }
                    return arr;
                }
                if (prop.Trim().StartsWith("{"))
                {
                    Dictionary<string, string> items = ParseDictionaryItems(prop.Trim());
                    Godot.Collections.Dictionary dict = new Godot.Collections.Dictionary();
                    foreach (var item in items)
                    {
                        dict.Add(item.Key, ConvertPropertyString(item.Value));
                    }
                    // GD.Print(dict);
                    return dict;
                }
                return SafeLoader.ConvertPropertyString(prop);
            }

            public PackedScene ToPackedScene(Dictionary<string, Script> scripts)
            {
                // return new PackedScene();
                Stopwatch sw = new Stopwatch();
                GD.Print($"Begin compile");
                sw.Start();
                Dictionary<string, Node> nodes = new Dictionary<string, Node>();
                Dictionary<string, Node> subscenes = new Dictionary<string, Node>();
                // first packed scenes
                foreach (var parNode in Nodes)
                {
                    if (!string.IsNullOrWhiteSpace(parNode.Instance))
                    {
                        NodePath path2 = new NodePath($"{parNode.Parent}/{parNode.Name}");
                        // GD.Print(path2);
                        Variant prop = ConvertPropertyString(parNode.Instance);
                        if (prop.VariantType == Variant.Type.Nil)
                        {
                            // GD.Print($"inst={parNode.Instance} path2={path2}");
                            continue;
                        }
                        Node node2 = prop.As<PackedScene>().Instantiate();
                        foreach (var kvp in parNode.Properties)
                        {
                            if (kvp.Key.StartsWith("script", StringComparison.OrdinalIgnoreCase))
                                continue;
                            // GD.PrintS(kvp.Key, kvp.Value);
                            node2.Set(kvp.Key, ConvertPropertyString(kvp.Value));
                        }
                        nodes.Add(path2.ToString(), node2);
                        subscenes.Add(path2.ToString(), node2);
                    }
                }
                // then non scenes
                foreach (var parNode in Nodes)
                {
                    if (!string.IsNullOrWhiteSpace(parNode.Instance))
                        continue;
                    NodePath path = new NodePath($"{parNode.Parent}/{parNode.Name}");
                    if (string.IsNullOrWhiteSpace(parNode.Type))
                    {
                        foreach (var scn in subscenes)
                        {
                            string path2 = path.ToString().Replace(scn.Key.TrimPrefix("./"), ".");
                            Node node2 = scn.Value.GetNodeOrNull(path2);
                            // GD.PrintS(path, scn.Key, path2, node2);
                            if (GodotObject.IsInstanceValid(node2))
                            {
                                foreach (var kvp in parNode.Properties)
                                {
                                    if (kvp.Key.StartsWith("script", StringComparison.OrdinalIgnoreCase))
                                        continue;
                                    // GD.PrintS(kvp.Key, kvp.Value);
                                    node2.Set(kvp.Key, ConvertPropertyString(kvp.Value));
                                }
                                break;
                            }
                        }
                        continue;
                    }
                    if (ClassDB.IsParentClass(parNode.Type, nameof(Script)) || string.IsNullOrWhiteSpace(parNode.Type))
                        parNode.Type = nameof(Node);
                    Node node = null;
                    foreach (var kvp in parNode.Properties)
                    {
                        Script scr = null;
                        if (kvp.Key.Equals("metadata/typename", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!scripts.TryGetValue(Marshalls.Base64ToVariant(kvp.Value).AsString().Trim('"'), out scr))
                                continue;
                        }
                        else
                            continue;
                        if (scr == null)
                            continue;
                        if (scr is CSharpScript css)
                            node = css.New().As<Node>();
                        if (scr is GDScript gds)
                            node = gds.New().As<Node>();
                    }
                    if (node == null)
                        node = ClassDB.Instantiate(parNode.Type).As<Node>();
                    node.Name = parNode.Name;
                    foreach (var kvp in parNode.Properties)
                    {
                        if (kvp.Key.StartsWith("script", StringComparison.OrdinalIgnoreCase))
                            continue;
                        // GD.PrintS(kvp.Key, kvp.Value);
                        node.Set(kvp.Key, ConvertPropertyString(kvp.Value));
                    }
                    if (string.IsNullOrEmpty(parNode.Parent))
                    {
                        nodes.Add(".", node);
                        continue;
                    }
                    nodes.Add(path.ToString(), node);
                }
                Node root = nodes.FirstOrDefault(x => x.Key == ".").Value;
                // GD.Print(root.Name);
                Node lastParent = root;
                List<string> paths = new List<string>(nodes.Keys/*nodes.Keys.OrderBy(x => new NodePath(x).GetNameCount())*/);
                int k = 0;
                int maxk = 20000;
                int prevCount = paths.Count;
                while (paths.Any(x => x != ".") /*&& k < maxk*/) // TODO: don't use while (if possible?)
                {
                    for (int i = 0; i < paths.Count; i++)
                    {
                        // GD.PrintS(paths[i], GetParent(paths[i]));
                        Node parent = root.GetNodeOrNull(GetParent(paths[i]));
                        if (GodotObject.IsInstanceValid(parent))
                        {
                            parent.AddChild(nodes[paths[i]]);
                            // GD.Print(paths[i]);
                            // /*
                            foreach (var ch in nodes[paths[i]].FindChildren("*", "", true, false))
                            {
                                ch.Owner = root;
                            }
                            // */
                            nodes[paths[i]].Owner = root;
                            paths.RemoveAt(i);
                            i--;
                        }
                        // else
                        //     GD.PrintS(paths[i], GetParent(paths[i]));
                            // GD.Print(paths[i]);
                    }
                    if (paths.Count == prevCount)
                    {
                        GD.Print("This should not happen");
                        k = maxk;
                        break;
                    }
                    prevCount = paths.Count;
                    k++;
                }
                if (k >= maxk)
                {
                    GD.Print(string.Join(" ", paths));
                }
                sw.Stop();
                GD.Print($"End compile {sw.ElapsedMilliseconds}ms");
                // GD.PrintS(string.Join(", ", paths));
                GD.Print($"Begin pack");
                sw.Restart();
                PackedScene scene = new PackedScene();
                scene.Pack(root);
                sw.Stop();
                GD.Print($"End pack {sw.ElapsedMilliseconds}ms");
                return scene;
            }
        }

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
            // if (path.ToString() == ".")
                // return new NodePath(".");
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

        public static Script LoadScript(string path)
        {
            foreach (var csType in typeof(ISandboxClass).Assembly.GetTypes())
            {
                if (csType.GetInterface(nameof(ISandboxClass)) == null)
                    continue;
                ScriptPathAttribute scrPath = csType.GetCustomAttribute<ScriptPathAttribute>();
                if (scrPath != null && scrPath.Path == path)
                {
                    CSharpScript script = ResourceLoader.Load<CSharpScript>(scrPath.Path);
                    return script;
                }
            }
            return null;
        }

        public static Script LoadScript<T>() where T : ISandboxClass
        {
            return LoadScript(typeof(T));
        }

        public static Script LoadScript(Type type)
        {
            if (type.GetInterface(nameof(ISandboxClass)) == null)
                return null;
            ScriptPathAttribute scrPath = type.GetCustomAttribute<ScriptPathAttribute>();
            if (scrPath != null)
            {
                CSharpScript script = ResourceLoader.Load<CSharpScript>(scrPath.Path);
                return script;
            }
            return null;
        }

        public ZipReader reader;
        public ParsedTscn world;
        public PackedScene scene;
        public Dictionary<string, Resource> cachedResources = new Dictionary<string, Resource>();
        public Dictionary<string, Script> validScripts = new Dictionary<string, Script>();

        public void ReadZip(string path)
        {
            if (charArr == null)
                charArr = new List<char>();
            reader = new ZipReader();
            Error err = reader.Open(path);
            if (err != Error.Ok)
            {
                GD.PrintErr(err);
                return;
            }
            try
            {
                if (reader.FileExists("world.txt"))
                {
                    string worldPath = Encoding.UTF8.GetString(reader.ReadFile("world.txt"));
                    world = ParseBin(path, reader.ReadFile(worldPath));
                    foreach (var resKvp in world.ExtResources)
                    {
                        // GD.PrintS(resKvp.Key, resKvp.Value);
                        Resource res = LoadFile(resKvp.Value);
                        world.LoadedExtResources.Add(resKvp.Key, res);
                    }
                    scene = world.ToPackedScene(validScripts);
                }
                else
                {
                    GD.PrintErr("Failed to find world.txt");
                }
            }
            finally
            {
                reader.Close();
            }
        }

        public Resource LoadFile(string path)
        {
            if (cachedResources.ContainsKey(path))
                return cachedResources[path];
            cachedResources.Add(path, null);
            string resPath = path.ReplaceN("res://", "");
            Resource res = null;
            if (resPath.GetExtension().Equals("scn", StringComparison.OrdinalIgnoreCase) || reader.FileExists(resPath.ReplaceN(resPath.GetExtension(), "scn")))
            {
                ParsedTscn tscn = ParseBin(path.ReplaceN(path.GetExtension(), "scn"), reader.ReadFile(resPath.ReplaceN(resPath.GetExtension(), "scn")));
                foreach (var resKvp in tscn.ExtResources)
                {
                    Resource res2 = LoadFile(resKvp.Value);
                    tscn.LoadedExtResources.Add(resKvp.Key, res2);
                }
                res = tscn.ToPackedScene(validScripts);
            }
            else if (resPath.GetExtension().Equals("tscn", StringComparison.OrdinalIgnoreCase) || reader.FileExists(resPath.ReplaceN(resPath.GetExtension(), "tscn")))
            {
                ParsedTscn tscn = ParseTscn(path.ReplaceN(path.GetExtension(), "tscn"), Encoding.UTF8.GetString(reader.ReadFile(resPath.ReplaceN(resPath.GetExtension(), "tscn"))));
                foreach (var resKvp in tscn.ExtResources)
                {
                    Resource res2 = LoadFile(resKvp.Value);
                    tscn.LoadedExtResources.Add(resKvp.Key, res2);
                }
                res = tscn.ToPackedScene(validScripts);
            }
            else if (reader.FileExists(resPath))
                res = ReadData(path, reader.ReadFile(resPath));
            // else if (reader.FileExists(resPath.ReplaceN(resPath.GetExtension(), "tres")))
            //     res = ReadData(path.ReplaceN(path.GetExtension(), "tres"), reader.ReadFile(resPath.ReplaceN(resPath.GetExtension(), "tres")));
            // else if (reader.FileExists(resPath.ReplaceN(resPath.GetExtension(), "asset")))
            //     res = ReadData(path.ReplaceN(path.GetExtension(), "asset"), reader.ReadFile(resPath.ReplaceN(resPath.GetExtension(), "asset")));
            else
                res = LoadScript(path);
            cachedResources[path] = res;
            return res;
        }

        public Resource ReadRes(string path, byte[] data)
        {
            DirAccess.MakeDirAbsolute("user://temp/");
            string tempPath = $"user://temp/{path.GetFile()}.asset";
            FileAccess file2 = FileAccess.Open(tempPath, FileAccess.ModeFlags.Write);
            if (file2 == null)
                GD.PrintErr($"{FileAccess.GetOpenError()} ({tempPath})");
            file2.StoreBuffer(data);
            file2.Close();
            FileAccess file = FileAccess.Open(tempPath, FileAccess.ModeFlags.Read);
            string type = file.GetPascalString();
            if (ClassDB.IsParentClass(type, nameof(Script)))
                return null;
                // type = nameof(Resource);
            Resource res = ClassDB.Instantiate(type).As<Resource>();
            while (file.GetPosition() < file.GetLength())
            {
                string key = file.GetPascalString();
                byte isRes = file.Get8();
                if (isRes == 0)
                {
                    Variant val = file.GetVar(false);
                    if (key == Resource.PropertyName.ResourcePath)
                        continue;
                    res.Set(key, val);
                }
                else if (isRes == 1)
                {
                    string resPath = file.GetPascalString();
                    if (key == Resource.PropertyName.ResourcePath)
                        continue;
                    Resource subRes = LoadFile(resPath);
                    res.Set(key, subRes);
                }
            }
            file.Close();
            return res;
        }

        public Resource ReadData(string path, byte[] data)
        {
            /*if (path.ToLower().EndsWith(".tres"))
            {
                ParsedTres tres = ParseTres(path, Encoding.UTF8.GetString(data));
                foreach (var resKvp in tres.ExtResources)
                {
                    string resPath = resKvp.Value.ReplaceN("res://", "");
                    if (!reader.FileExists(resPath))
                        continue;
                    Resource res = ReadData(resKvp.Value, reader.ReadFile(resPath));
                    tres.LoadedExtResources.Add(resKvp.Key, res);
                }
                return tres.ToResource();
            }
            else*/
            /*
            if (path.ToLower().EndsWith(".asset"))
            {
                DirAccess.MakeDirAbsolute("user://temp/");
                string tempPath = $"user://temp/{path.GetFile()}.asset";
                FileAccess file2 = FileAccess.Open(tempPath, FileAccess.ModeFlags.Write);
                if (file2 == null)
                    GD.PrintErr($"{FileAccess.GetOpenError()} ({tempPath})");
                file2.StoreBuffer(data);
                file2.Close();
                FileAccess file = FileAccess.Open(tempPath, FileAccess.ModeFlags.Read);
                string type = file.GetPascalString();
                if (ClassDB.IsParentClass(type, nameof(Script)))
                    return null;
                    // type = nameof(Resource);
                Resource res = ClassDB.Instantiate(type).As<Resource>();
                while (file.GetPosition() < file.GetLength())
                {
                    string key = file.GetPascalString();
                    byte isRes = file.Get8();
                    if (isRes == 0)
                    {
                        Variant val = file.GetVar(false);
                        if (key == Resource.PropertyName.ResourcePath)
                            continue;
                        res.Set(key, val);
                    }
                    else if (isRes == 1)
                    {
                        string resPath = file.GetPascalString();
                        if (key == Resource.PropertyName.ResourcePath)
                            continue;
                        Resource subRes = LoadFile(resPath);
                        res.Set(key, subRes);
                    }
                }
                file.Close();
                return res;
            }
            else
            */
            {
                Image img = new Image();
                switch (path.GetExtension().ToLower())
                {
                    case "png":
                        img.LoadPngFromBuffer(data);
                        break;
                    case "jpg":
                    case "jpeg":
                        img.LoadJpgFromBuffer(data);
                        break;
                    case "svg":
                        img.LoadSvgFromBuffer(data);
                        break;
                    case "bmp":
                        img.LoadBmpFromBuffer(data);
                        break;
                    case "webp":
                        img.LoadWebpFromBuffer(data);
                        break;
                    case "tga":
                        img.LoadTgaFromBuffer(data);
                        break;
                    case "ktx":
                        img.LoadKtxFromBuffer(data);
                        break;
                    default:
                        return ReadRes(path, data);
                }
                img.GenerateMipmaps();
                // img.Compress(Image.CompressMode.S3Tc, Image.CompressSource.Generic);
                ImageTexture tex = ImageTexture.CreateFromImage(img);
                return tex;
            }
        }

        public static Variant ConvertPropertyString(string prop)
        {
            // prevent objects from loading
            if (prop.Trim().Contains("Object(", StringComparison.OrdinalIgnoreCase))
                return new Variant();
            // prevent long parsing times
            // if (prop.Length >= 4096 * 2)
            //     return new Variant();
            Variant va = Marshalls.Base64ToVariant(prop);
            return va;
            foreach (var key in Enum.GetNames<Variant.Type>())
            {
                if (prop.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    int start = prop.Find("(");
                    int end = prop.Find(")", start);
                    string propData = prop.Substr(start + 1, end - start + 1);
                    GD.PrintS(propData);
                    float[] vals = propData.SplitFloats(",");
                    switch (Enum.Parse<Variant.Type>(key))
                    {
                        case Variant.Type.Vector2:
                            return new Vector2(vals[0], vals[1]);
                        case Variant.Type.Vector2I:
                            return new Vector2I((int)vals[0], (int)vals[1]);
                        case Variant.Type.Rect2:
                        case Variant.Type.Rect2I:
                            throw new NotImplementedException();
                        case Variant.Type.Vector3:
                            return new Vector3(vals[0], vals[1], vals[2]);
                        case Variant.Type.Vector3I:
                            return new Vector3I((int)vals[0], (int)vals[1], (int)vals[2]);
                        case Variant.Type.Transform2D:
                            return new Transform2D(vals[0], vals[1], vals[2],
                                vals[3], vals[4], vals[5]);
                        case Variant.Type.Vector4:
                        case Variant.Type.Vector4I:
                        case Variant.Type.Plane:
                            throw new NotImplementedException();
                        case Variant.Type.Quaternion:
                            return new Quaternion(vals[0], vals[1], vals[2], vals[3]);
                        case Variant.Type.Aabb:
                            return new Aabb(new Vector3(vals[0], vals[1], vals[2]), new Vector3(vals[3], vals[4], vals[5]));
                        case Variant.Type.Basis:
                            throw new NotImplementedException();
                        case Variant.Type.Transform3D:
                            return new Transform3D(vals[0], vals[1], vals[2],
                                vals[3], vals[4], vals[5],
                                vals[6], vals[7], vals[8],
                                vals[9], vals[10], vals[11]);
                        case Variant.Type.Projection:
                        case Variant.Type.Color:
                        case Variant.Type.NodePath:
                            throw new NotImplementedException();
                    }
                }
            }
            return new Variant();
        }

        public static ParsedTscn ParseBin(string path, byte[] data)
        {
            Stopwatch sw = new Stopwatch();
            GD.Print("Begin parse");
            sw.Start();
            ParsedTscn tscn = new ParsedTscn();

            var dict = GD.BytesToVar(data).AsGodotDictionary();
            if (dict.ContainsKey("ext_resources"))
                foreach (var tresDict in dict["ext_resources"].AsGodotArray<Godot.Collections.Dictionary>())
                {
                    tscn.ExtResources.TryAdd(tresDict["id"].AsString(), tresDict["path"].AsString());
                }
            if (dict.ContainsKey("sub_resources"))
                foreach (var tresDict in dict["sub_resources"].AsGodotArray<Godot.Collections.Dictionary>())
                {
                    ParsedSubTres tres = new ParsedSubTres();
                    foreach (var kvp in tresDict["props"].AsGodotDictionary())
                        tres.Properties.Add(kvp.Key.AsString(), kvp.Value.AsString());
                    tres.Type = tresDict["type"].AsString();
                    tscn.SubResources.TryAdd(tresDict["id"].AsString(), tres);
                }
            if (dict.ContainsKey("nodes"))
                foreach (var nodeDict in dict["nodes"].AsGodotArray<Godot.Collections.Dictionary>())
                {
                    ParsedNode node = new ParsedNode();
                    foreach (var kvp in nodeDict["props"].AsGodotDictionary())
                        node.Properties.Add(kvp.Key.AsString(), kvp.Value.AsString());
                    node.Name = nodeDict["name"].AsString();
                    if (nodeDict.ContainsKey("type"))
                        node.Type = nodeDict["type"].AsString();
                    if (nodeDict.ContainsKey("parent"))
                        node.Parent = nodeDict["parent"].AsString();
                    if (nodeDict.ContainsKey("instance"))
                        node.Instance = nodeDict["instance"].AsString();
                    tscn.Nodes.Add(node);
                }

            sw.Stop();
            GD.Print($"End parse {sw.ElapsedMilliseconds}ms");
            return tscn;
        }

        public static ParsedTscn ParseTscn(string path, string data)
        {
            if (!data.StartsWith("[gd_scene"))
            {
                GD.PrintErr("World isn't a scene!");
                return null;
            }
            data = data.Replace("\r", null);
            Stopwatch sw = new Stopwatch();
            GD.Print("Begin parse");
            sw.Start();
            ParsedTscn tscn = new ParsedTscn();
            for (int i = 0; i < data.Length; i++)
            {
                var attribs = ParseTag(data, i, out var tag, out var offset);
                i = offset;
                switch (tag.ToLower())
                {
                    case "ext_resource":
                        tscn.ExtResources.Add(attribs["id"], attribs["path"]);
                        break;
                    case "sub_resource":
                        ParsedSubTres sub = new ParsedSubTres();
                        while (ParseProperty(data, i, out var key, out var val, out var off))
                        {
                            i = off;
                            sub.Properties.Add(key, val);
                        }
                        sub.Type = attribs["type"];
                        tscn.SubResources.Add(attribs["id"], sub);
                        break;
                    case "node":
                        ParsedNode node = new ParsedNode();
                        while (ParseProperty(data, i, out var key, out var val, out var off))
                        {
                            i = off;
                            node.Properties.Add(key, val);
                        }
                        node.Name = attribs["name"];
                        if (attribs.ContainsKey("type"))
                            node.Type = attribs["type"];
                        if (attribs.ContainsKey("parent"))
                            node.Parent = attribs["parent"];
                        if (attribs.ContainsKey("instance"))
                            node.Instance = attribs["instance"];
                        tscn.Nodes.Add(node);
                        break;
                    case "connection":
                        // TODO
                        break;
                    default:
                        break;
                }
            }
            sw.Stop();
            GD.Print($"End parse {sw.ElapsedMilliseconds}ms");
            return tscn;
        }

        [ThreadStatic]
        public static List<char> charArr = new List<char>();

        public static ParsedTres ParseTres(string path, string data)
        {
            if (!data.StartsWith("[gd_resource"))
                return null;
            data = data.Replace("\r", null);
            ParsedTres tres = new ParsedTres();
            for (int i = 0; i < data.Length; i++)
            {
                var attribs = ParseTag(data, i, out var tag, out var offset);
                i = offset;
                switch (tag.ToLower())
                {
                    case "gd_resource":
                        tres.Type = attribs["type"].Replace("\"", null);
                        break;
                    case "ext_resource":
                        tres.ExtResources.Add(attribs["id"].Replace("\"", null), attribs["path"].Replace("\"", null));
                        break;
                    case "resource":
                        while (ParseProperty(data, i, out var key, out var val, out var off))
                        {
                            i = off;
                            tres.Properties.Add(key, val);
                        }
                        break;
                    default:
                        break;
                }
            }
            return tres;
        }

        public static bool ParseProperty(string data, int offset, out string key, out string value, out int newOffset)
        {
            key = string.Empty;
            value = string.Empty;
            newOffset = offset;
            
            int eqIdx = -1;
            for (int i = offset; i < data.Length; i++)
            {
                if (data[i] == '[')
                {
                    return false;
                }
                if (data[i] == '=')
                {
                    eqIdx = i;
                    break;
                }
            }
            if (eqIdx == -1)
            {
                return false;
            }
            key = data.Substr(offset, eqIdx - 1 - offset).Replace("\n", null);
            
            int escapes = 0;
            bool isQuote = false;
            int escapesArray = 0;
            int escapesDict = 0;

            // List<char> charArr = new List<char>();
            charArr.Clear();

            for (int i = eqIdx + 2; i < data.Length; i++)
            {
                newOffset = i;

                // check for end
                if (escapes == 0 && escapesArray == 0 && escapesDict == 0 && data[i] == '\n')
                {
                    break;
                }

                if (isQuote || data[i] != '\n')
                {
                    charArr.Add(data[i]);
                }

                // check for string
                if (data[i] == '"')
                {
                    if (isQuote)
                        escapes--;
                    else
                        escapes++;
                    isQuote = !isQuote;
                }

                // check for arrays
                {
                    if (data[i] == '[')
                    {
                        escapesArray++;
                    }
                    if (data[i] == ']')
                    {
                        escapesArray--;
                    }
                }

                // check for dictionaries
                {
                    if (data[i] == '{')
                    {
                        escapesDict++;
                    }
                    if (data[i] == '}')
                    {
                        escapesDict--;
                    }
                }
            }

            // value = new string(charArr.ToArray());
            value = new string(charArr.ToArray());
            return true;
        }

        public static Dictionary<string, string> ParseDictionaryItems(string data)
        {
            int escapes = 0;
            bool isQuote = false;
            int escapesArray = 0;
            int escapesDict = 0;
            bool hasDict = false;

            // List<char> charArr = new List<char>();
            charArr.Clear();
            Dictionary<string, string> splits = new Dictionary<string, string>();
            string key = string.Empty;

            for (int i = 0; i < data.Length; i++)
            {
                // check for end
                if (escapes == 0 && escapesArray == 0 && escapesDict == 0 && hasDict)
                {
                    break;
                }

                if (escapesDict == 1 && escapes == 0 && escapesArray == 0 && data[i] == ':')
                {
                    key = new string(charArr.ToArray()).Replace("\"", null);
                    charArr.Clear();
                    continue;
                }
                if (escapesDict == 1 && escapes == 0 && escapesArray == 0 && data[i] == ',')
                {
                    splits.TryAdd(key, new string(charArr.ToArray()));
                    charArr.Clear();
                    continue;
                }

                if (data[i] != '{' && data[i] != '}')
                    charArr.Add(data[i]);

                // check for string
                if (data[i] == '"')
                {
                    if (isQuote)
                        escapes--;
                    else
                        escapes++;
                    isQuote = !isQuote;
                }

                // check for arrays
                {
                    if (data[i] == '[')
                    {
                        escapesArray++;
                    }
                    if (data[i] == ']')
                    {
                        escapesArray--;
                    }
                }

                // check for dictionaries
                {
                    if (data[i] == '{')
                    {
                        escapesDict++;
                        hasDict = true;
                    }
                    if (data[i] == '}')
                    {
                        escapesDict--;
                    }
                }

                // check for objects
                {
                    if (data[i] == '(')
                    {
                        escapesDict++;
                    }
                    if (data[i] == ')')
                    {
                        escapesDict--;
                    }
                }
            }
            splits.TryAdd(key, new string(charArr.ToArray()));

            return splits;
        }

        public static List<string> ParseArrayItems(string data)
        {
            int escapes = 0;
            bool isQuote = false;
            int escapesArray = 0;
            int escapesDict = 0;
            bool hasArray = false;

            // List<char> charArr = new List<char>();
            charArr.Clear();
            List<string> splits = new List<string>();

            for (int i = 0; i < data.Length; i++)
            {
                // check for end
                if (escapes == 0 && escapesArray == 0 && escapesDict == 0 && hasArray)
                {
                    break;
                }

                if (escapesArray == 1 && escapes == 0 && escapesDict == 0 && data[i] == ',')
                {
                    splits.Add(new string(charArr.ToArray()));
                    charArr.Clear();
                    continue;
                }

                if (data[i] != '[' && data[i] != ']')
                    charArr.Add(data[i]);

                // check for string
                if (data[i] == '"')
                {
                    if (isQuote)
                        escapes--;
                    else
                        escapes++;
                    isQuote = !isQuote;
                }

                // check for arrays
                {
                    if (data[i] == '[')
                    {
                        escapesArray++;
                        hasArray = true;
                    }
                    if (data[i] == ']')
                    {
                        escapesArray--;
                    }
                }

                // check for dictionaries
                {
                    if (data[i] == '{')
                    {
                        escapesDict++;
                    }
                    if (data[i] == '}')
                    {
                        escapesDict--;
                    }
                }

                // check for objects
                {
                    if (data[i] == '(')
                    {
                        escapesDict++;
                    }
                    if (data[i] == ')')
                    {
                        escapesDict--;
                    }
                }
            }
            splits.Add(new string(charArr.ToArray()));

            return splits;
        }

        public static Dictionary<string, string> ParseTag(string data, int offset, out string tag, out int newOffset)
        {
            int idx = data.Find('[', offset);
            tag = string.Empty;
            newOffset = offset;
            if (idx == -1)
                return new Dictionary<string, string>();
            Dictionary<string, string> attribs = new Dictionary<string, string>();
            int escapes = 0;
            bool isQuote = false;
            int state = 0; // 0 = tag, 1 = key, 2 = value
            string key = string.Empty;
            string val = string.Empty;
            for (int i = idx + 1; i < data.Length; i++)
            {
                // check for end
                if (escapes == 0 && data[i] == ']')
                {
                    if (state == 2)
                        attribs.Add(key, val);
                    newOffset = i + 1;
                    break;
                }

                switch (state)
                {
                    case 0:
                        if (data[i] == ' ')
                            state = 1; // key state
                        else
                            tag += data[i];
                        break;
                    case 1:
                        if (data[i] == '=')
                            state = 2; // value state
                        else if (data[i] != '"')
                            key += data[i];
                        break;
                    case 2:
                        if (escapes == 0 && data[i] == ' ')
                        {
                            state = 1; // key state
                            attribs.Add(key, val);
                            key = string.Empty;
                            val = string.Empty;
                        }
                        else if (data[i] != '"')
                            val += data[i];
                        break;
                }

                // check for string
                if (data[i] == '"')
                {
                    if (isQuote)
                        escapes--;
                    else
                        escapes++;
                    isQuote = !isQuote;
                }
            }

            return attribs;
        }
    }
}
