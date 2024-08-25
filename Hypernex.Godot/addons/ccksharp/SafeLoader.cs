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
    public partial class SafeLoader : IDisposable
    {
        public class ParsedSubTres
        {
            public string Type;
            public Dictionary<string, Variant> Properties = new Dictionary<string, Variant>();
        }

        public class ParsedTres
        {
            public string Type;
            public Dictionary<string, ParsedSubTres> SubResources = new Dictionary<string, ParsedSubTres>();
            public Dictionary<string, string> ExtResources = new Dictionary<string, string>();
            public Dictionary<string, Variant> Properties = new Dictionary<string, Variant>();

            public Dictionary<string, Resource> LoadedSubResources = new Dictionary<string, Resource>();
            public Dictionary<string, Resource> LoadedExtResources = new Dictionary<string, Resource>();

            public Dictionary<string, Script> validScripts = new Dictionary<string, Script>();
            public Resource cachedRes;
            public string zippath;

            public ParsedTres(string path)
            {
                zippath = path;
            }

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
                    Resource res = CreateResource(zippath, sub.Type, sub.Properties, validScripts);
                    foreach (var kvp in sub.Properties)
                    {
                        if (kvp.Key.StartsWith("script", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (kvp.Key == Resource.PropertyName.ResourcePath)
                            continue;
                        if (sub.Type == nameof(Resource))
                        {
                            res.SetMeta(kvp.Key, ConvertProperty(kvp.Value));
                        }
                        else
                        {
                            res.Set(kvp.Key, ConvertProperty(kvp.Value));
                        }
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

            public Variant ConvertProperty(Variant prop)
            {
                switch (prop.VariantType)
                {
                    default:
                        return ConvertPropertyString(prop.AsString());
                    case Variant.Type.Array:
                        Godot.Collections.Array arr = prop.AsGodotArray();
                        Godot.Collections.Array arrOut = new Godot.Collections.Array();
                        for (int i = 0; i < arr.Count; i++)
                        {
                            arrOut.Add(ConvertProperty(arr[i]));
                        }
                        return arrOut;
                    case Variant.Type.Dictionary:
                        Godot.Collections.Dictionary dict = prop.AsGodotDictionary();
                        Godot.Collections.Dictionary dictOut = new Godot.Collections.Dictionary();
                        foreach (var item in dict)
                        {
                            dictOut.Add(item.Key.AsString(), ConvertProperty(item.Value));
                        }
                        return dictOut;
                }
            }

            public Resource ToResource(Dictionary<string, Script> scripts)
            {
                validScripts = scripts;
                if (!GodotObject.IsInstanceValid(cachedRes))
                {
                    cachedRes = CreateResource(zippath, Type, Properties, scripts);
                    foreach (var kvp in Properties)
                    {
                        if (kvp.Key.Equals("script", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (kvp.Key == Resource.PropertyName.ResourcePath)
                            continue;
                        cachedRes.Set(kvp.Key, ConvertProperty(kvp.Value));
                    }
                }
                return cachedRes;
            }
        }

        public class ParsedNode
        {
            public string Name;
            public string Parent;
            public string Instance;
            public string Type;
            public Dictionary<string, Variant> Properties = new Dictionary<string, Variant>();
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

            public Dictionary<string, Script> validScripts = new Dictionary<string, Script>();
            public string zippath;

            public ParsedTscn(string path)
            {
                zippath = path;
            }

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
                    Resource res = CreateResource(zippath, sub.Type, sub.Properties, validScripts);
                    foreach (var kvp in sub.Properties)
                    {
                        if (kvp.Key.StartsWith("script", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (kvp.Key == Resource.PropertyName.ResourcePath)
                            continue;
                        res.Set(kvp.Key, ConvertProperty(kvp.Value));
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

            public Variant ConvertProperty(Variant prop)
            {
                switch (prop.VariantType)
                {
                    default:
                        return ConvertPropertyString(prop.AsString());
                    case Variant.Type.Array:
                        Godot.Collections.Array arr = prop.AsGodotArray();
                        Godot.Collections.Array arrOut = new Godot.Collections.Array();
                        for (int i = 0; i < arr.Count; i++)
                        {
                            arrOut.Add(ConvertProperty(arr[i]));
                        }
                        return arrOut;
                    case Variant.Type.Dictionary:
                        Godot.Collections.Dictionary dict = prop.AsGodotDictionary();
                        Godot.Collections.Dictionary dictOut = new Godot.Collections.Dictionary();
                        foreach (var item in dict)
                        {
                            dictOut.Add(item.Key.AsString(), ConvertProperty(item.Value));
                        }
                        return dictOut;
                }
            }

            public PackedScene ToPackedScene(Dictionary<string, Script> scripts)
            {
                validScripts = scripts;
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
                            if (kvp.Key.StartsWith("name", StringComparison.OrdinalIgnoreCase))
                                continue;
                            // GD.PrintS(kvp.Key, kvp.Value);
                            node2.Set(kvp.Key, ConvertProperty(kvp.Value));
                        }
                        node2.Name = parNode.Name;
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
                                    node2.Set(kvp.Key, ConvertProperty(kvp.Value));
                                }
                                break;
                            }
                        }
                        continue;
                    }
                    if (ClassDB.IsParentClass(parNode.Type, nameof(Resource)) || string.IsNullOrWhiteSpace(parNode.Type))
                        parNode.Type = nameof(Node);
                    Node node = null;
                    foreach (var kvp in parNode.Properties)
                    {
                        Script scr = null;
                        if (kvp.Key.Equals("metadata/typename", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!scripts.TryGetValue(ConvertProperty(kvp.Value).AsString().Trim('"'), out scr))
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
                        node.Set(kvp.Key, ConvertProperty(kvp.Value));
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
                int maxk = 200;
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
                        // k = maxk;
                        k++;
                        if (k >= maxk)
                            break;
                    }
                    prevCount = paths.Count;
                    // k++;
                }
                if (k >= maxk)
                {
                    GD.Print(string.Join(" ", paths));
                }
                sw.Stop();
                GD.Print($"End compile {sw.ElapsedMilliseconds}ms");
                // GD.PrintS(string.Join(", ", paths));
                string name = root.Name;
                GD.Print($"Begin pack {name}");
                sw.Restart();
                PackedScene scene = new PackedScene();
                scene.Pack(root);
                foreach (var node in root.FindChildren("*", owned: false))
                {
                    // if (GodotObject.IsInstanceValid(node))
                        // node.Free();
                }
                if (GodotObject.IsInstanceValid(root))
                    root.QueueFree();
                /*
                foreach (var node in subscenes)
                {
                    if (GodotObject.IsInstanceValid(node.Value))
                    {
                        node.Value.Free();
                    }
                }
                foreach (var node in nodes)
                {
                    if (GodotObject.IsInstanceValid(node.Value))
                        node.Value.Free();
                }
                */
                sw.Stop();
                GD.Print($"End pack {name} {sw.ElapsedMilliseconds}ms");
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

        public static Resource CreateResource(string path, string type, Dictionary<string, Variant> properties, Dictionary<string, Script> scripts)
        {
            if (ClassDB.IsParentClass(type, nameof(Script)) || ClassDB.IsParentClass(type, nameof(PackedScene)) || ClassDB.IsParentClass(type, nameof(Node)))
                return null;
            Resource res = null;
            foreach (var kvp in properties)
            {
                Script scr = null;
                if (kvp.Key.Equals("metadata/typename", StringComparison.OrdinalIgnoreCase))
                {
                    if (!scripts.TryGetValue(ConvertPropertyString(kvp.Value.AsString()).AsString().Trim('"'), out scr))
                        continue;
                }
                else
                    continue;
                if (scr == null)
                    continue;
                if (scr is CSharpScript css)
                    res = css.New().As<Resource>();
                if (scr is GDScript gds)
                    res = gds.New().As<Resource>();
            }
            if (!GodotObject.IsInstanceValid(res))
                res = ClassDB.Instantiate(type).As<Resource>();
            if (!GodotObject.IsInstanceValid(res))
                return null;
            loadedResources[path].Add(res);
            /*
            foreach (var kvp in properties)
            {
                if (kvp.Key == Resource.PropertyName.ResourcePath)
                    continue;
                res.Set(kvp.Key, kvp.Value);
            }
            */
            return res;
        }

        public ZipReader reader;
        public ParsedTscn world;
        public PackedScene scene;
        public string zippath;
        public Dictionary<string, Resource> cachedResources = new Dictionary<string, Resource>();
        public Dictionary<string, Script> validScripts = new Dictionary<string, Script>();
        public static Dictionary<string, List<Resource>> loadedResources = new Dictionary<string, List<Resource>>();

        public void Unload()
        {
            scene = null;
            if (loadedResources.TryGetValue(zippath, out var resources))
            {
                foreach (var res in resources)
                {
                    if (res is Mesh || res is Texture || res is Shader || res is Material || res is Image)
                        RenderingServer.FreeRid(res.GetRid());
                }
                resources.Clear();
                loadedResources.Remove(zippath);
            }
        }

        public void Dispose()
        {
            reader.Dispose();
            Unload();
        }

        public void ReadZip(string path)
        {
            if (!string.IsNullOrEmpty(zippath))
            {
                return;
            }
            zippath = path;
            Unload();
            loadedResources.Add(path, new List<Resource>());
            if (charArr == null)
                charArr = new List<char>();
            reader = new ZipReader();
            Error err = reader.Open(path);
            if (err != Error.Ok)
            {
                GD.PrintErr(err);
                reader.Close();
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
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
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
                ParsedTscn tscn = ParseBin(zippath, reader.ReadFile(resPath.ReplaceN(resPath.GetExtension(), "scn")));
                foreach (var resKvp in tscn.ExtResources)
                {
                    Resource res2 = LoadFile(resKvp.Value);
                    tscn.LoadedExtResources.Add(resKvp.Key, res2);
                }
                res = tscn.ToPackedScene(validScripts);
            }
            else if (reader.FileExists(resPath))
                res = ReadData(path, reader.ReadFile(resPath));
            else
                res = LoadScript(path);
            cachedResources[path] = res;
            return res;
        }

        public ParsedTres ParseBinRes(string path, byte[] data)
        {
            Stopwatch sw = new Stopwatch();
            GD.Print("Begin parse");
            sw.Start();
            ParsedTres tscn = new ParsedTres(zippath);

            var dict = GD.BytesToVar(data).AsGodotDictionary();
            var rootDict = dict["resource"].AsGodotDictionary();
            tscn.Type = rootDict["type"].AsString();
            foreach (var kvp in rootDict["props"].AsGodotDictionary())
            {
                tscn.Properties.Add(kvp.Key.AsString(), kvp.Value);
            }
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
                    {
                        tres.Properties.Add(kvp.Key.AsString(), kvp.Value);
                    }
                    tres.Type = tresDict["type"].AsString();
                    tscn.SubResources.TryAdd(tresDict["id"].AsString(), tres);
                }

            sw.Stop();
            GD.Print($"End parse {sw.ElapsedMilliseconds}ms");
            return tscn;
        }

        public Resource ReadData(string path, byte[] data)
        {
            Image img = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
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
                case "ttf":
                case "otf":
                case "woff":
                case "woff2":
                case "pfb":
                case "pfm":
                {
                    var font = new FontFile();
                    var file = FileAccess.Open($"user://{path.GetFile()}", FileAccess.ModeFlags.Write);
                    file.StoreBuffer(data);
                    file.Close();
                    font.LoadDynamicFont($"user://{path.GetFile()}");
                    return font;
                }
                default:
                {
                    var tscn = ParseBinRes(path, data);
                    foreach (var resKvp in tscn.ExtResources)
                    {
                        Resource res2 = LoadFile(resKvp.Value);
                        tscn.LoadedExtResources.Add(resKvp.Key, res2);
                    }
                    return tscn.ToResource(validScripts);
                }
            }
            img.GenerateMipmaps();
            if (OS.GetName().Equals("Android", StringComparison.OrdinalIgnoreCase))
                    img.Compress(Image.CompressMode.Etc2, Image.CompressSource.Generic);
            ImageTexture tex = ImageTexture.CreateFromImage(img);
            loadedResources[zippath].Add(tex);
            loadedResources[zippath].Add(img);
            return tex;
        }

        public static Variant ConvertPropertyString(string prop)
        {
            // prevent objects from loading
            if (prop.Trim().Contains("Object(", StringComparison.OrdinalIgnoreCase))
                return new Variant();
            Variant va = Marshalls.Base64ToVariant(prop);
            return va;
        }

        public static ParsedTscn ParseBin(string zippath, byte[] data)
        {
            Stopwatch sw = new Stopwatch();
            GD.Print("Begin parse");
            sw.Start();
            ParsedTscn tscn = new ParsedTscn(zippath);

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
                    {
                        tres.Properties.Add(kvp.Key.AsString(), kvp.Value);
                    }
                    tres.Type = tresDict["type"].AsString();
                    tscn.SubResources.TryAdd(tresDict["id"].AsString(), tres);
                }
            if (dict.ContainsKey("nodes"))
                foreach (var nodeDict in dict["nodes"].AsGodotArray<Godot.Collections.Dictionary>())
                {
                    ParsedNode node = new ParsedNode();
                    foreach (var kvp in nodeDict["props"].AsGodotDictionary())
                        node.Properties.Add(kvp.Key.AsString(), kvp.Value);
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

        [ThreadStatic]
        public static List<char> charArr = new List<char>();

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
