using Godot;
using Godot.Collections;
using Hypernex.CCK.GodotVersion.Extensions;
using Hypernex.Game;

namespace Hypernex.CCK.GodotVersion
{
    public partial class GltfCckExt : GltfDocumentExtension
    {
        public const string EXT_NAME = "HYPERNEX_extras";
        public const string EXTS = "extensions";

        public override string[] _GetSupportedExtensions()
        {
            return new string[] { EXT_NAME };
        }

        public override Error _ImportPreflight(GltfState state, string[] extensions)
        {
            return Error.Ok;
        }

        public override Error _ImportNode(GltfState state, GltfNode gltfNode, Dictionary json, Node node)
        {
            return Error.Ok;
        }

        public override Error _ImportPost(GltfState state, Node root)
        {
            var nodes = state.GetNodes();
            var children = root.FindChildren("*");
            foreach (var node in children)
            {
                int idx = state.GetNodeIndex(node);
                if (idx == -1)
                    continue;
                GltfNode gltfNode = nodes[idx];
                Variant val = gltfNode.GetAdditionalData(EXT_NAME);
                if (val.VariantType != Variant.Type.Dictionary)
                    return Error.Ok;
                var dict = val.AsGodotDictionary();
                if (dict.TryGetBool("is_3d", out bool is3d))
                {
                    if (!is3d)
                    {
                        Node node3d = node;
                        Node newNode = node3d.GetChild(0);
                        newNode.Owner = null;
                        node3d.RemoveChild(newNode);
                        node3d.ReplaceBy(newNode);
                        node3d.QueueFree();
                    }
                }
            }
            return Error.Ok;
        }

        public override Error _ExportNode(GltfState state, GltfNode gltfNode, Dictionary json, Node node)
        {
            var data = gltfNode.GetAdditionalData(EXT_NAME);
            if (data.VariantType == Variant.Type.Nil)
                return Error.Ok;
            if (!json.ContainsKey(EXTS))
                json.Add(EXTS, new Dictionary());
            json[EXTS].AsGodotDictionary()[EXT_NAME] = gltfNode.GetAdditionalData(EXT_NAME);
            return Error.Ok;
        }

        public override Error _ParseNodeExtensions(GltfState state, GltfNode gltfNode, Dictionary extensions)
        {
            if (extensions.TryGetDict(EXT_NAME, out Dictionary dict))
                gltfNode.SetAdditionalData(EXT_NAME, dict);
            return Error.Ok;
        }

        public override void _ConvertSceneNode(GltfState state, GltfNode gltfNode, Node sceneNode)
        {
            var dict = new Dictionary();
            if (sceneNode is not Node3D)
            {
                dict["is_3d"] = false;
            }
            state.AddUsedExtension(EXT_NAME, false);
            gltfNode.SetAdditionalData(EXT_NAME, dict);
        }

        public override Error _ImportPostParse(GltfState state)
        {
            var nodes = state.GetNodes();
            for (int i = 0; i < nodes.Count; i++)
            {
                Variant val = nodes[i].GetAdditionalData(EXT_NAME);
                if (val.VariantType != Variant.Type.Dictionary)
                    continue;
                var dict = val.AsGodotDictionary();
                if (dict.TryGetBool("is_3d", out bool is3d))
                {
                    if (!is3d)
                    {
                        // nodes[i].Parent = -1;
                        continue;
                    }
                }
            }
            return Error.Ok;
        }

        public override Node3D _GenerateSceneNode(GltfState state, GltfNode gltfNode, Node sceneParent)
        {
            Variant val = gltfNode.GetAdditionalData(EXT_NAME);
            if (val.VariantType != Variant.Type.Dictionary)
                return null;
            var dict = val.AsGodotDictionary();
            if (dict.TryGetBool("is_3d", out bool is3d))
            {
                if (!is3d)
                {
                    Node3D node = new Node3D();
                    node.AddChild(new Node()
                    {
                        Name = gltfNode.OriginalName
                    }, true);
                    return node;
                }
            }
            return null;
        }
    }

    [Tool]
    public partial class GltfSceneLoader : ISceneProvider
    {
        public static GltfAvatarExt AvatarExt;
        public static GltfVideoExt VideoExt;
        public static GltfAudioExt AudioExt;
        public static GltfGrabbableExt GrabbableExt;

        public static void Init()
        {
            AvatarExt ??= new GltfAvatarExt();
            VideoExt ??= new GltfVideoExt();
            AudioExt ??= new GltfAudioExt();
            GrabbableExt ??= new GltfGrabbableExt();
            GltfDocument.RegisterGltfDocumentExtension(AvatarExt);
            GltfDocument.RegisterGltfDocumentExtension(VideoExt);
            GltfDocument.RegisterGltfDocumentExtension(AudioExt);
            GltfDocument.RegisterGltfDocumentExtension(GrabbableExt);
        }

        public static void DeInit()
        {
            GltfDocument.UnregisterGltfDocumentExtension(AvatarExt);
            GltfDocument.UnregisterGltfDocumentExtension(VideoExt);
            GltfDocument.UnregisterGltfDocumentExtension(AudioExt);
            GltfDocument.UnregisterGltfDocumentExtension(GrabbableExt);
        }

        public void Dispose()
        {
        }

        public PackedScene LoadFromFile(string filePath)
        {
            Error err = Error.Ok;
            // ZipReader zip = new ZipReader();
            // err = zip.Open(filePath);
            GltfDocument doc = new GltfDocument();
            GltfState state = new GltfState();
            // err = doc.AppendFromBuffer(zip.ReadFile("world.glb"), string.Empty, state);
            // err = doc.AppendFromBuffer(FileAccess.GetFileAsBytes(filePath), string.Empty, state);
            err = doc.AppendFromFile(filePath, state);
            Node node = doc.GenerateScene(state);
            PackedScene scene = new PackedScene();
            err = scene.Pack(node);
            return scene;
        }

        public void SaveToFile(string filePath, PackedScene scene)
        {
            Error err = Error.Ok;
            GltfDocument doc = new GltfDocument();
            // doc.LossyQuality = 1f;
            GltfState state = new GltfState();
            Node n = scene.Instantiate();
            err = doc.AppendFromScene(n, state);
            GD.PrintErr("AppendFromScene:", err);
            err = doc.WriteToFilesystem(state, filePath);
            // byte[] data = doc.GenerateBuffer(state);
            // var fs = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
            // fs.StoreBuffer(data);
            // fs.Close();
            GD.PrintErr("WriteToFilesystem:", err);
            n.Free();
        }
    }
}
