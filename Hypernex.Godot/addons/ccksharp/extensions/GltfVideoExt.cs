using System;
using Godot;
using Godot.Collections;
using Hypernex.CCK.GodotVersion.Classes;

namespace Hypernex.CCK.GodotVersion.Extensions
{
    [Tool]
    [GlobalClass]
    public partial class GltfVideoExt : GltfDocumentExtension
    {
        public const string EXT_NAME = "HYPERNEX_video_player";
        public const string EXTS = "extensions";

        public override string[] _GetSupportedExtensions()
        {
            return new string[] { EXT_NAME };
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

        public override void _ConvertSceneNode(GltfState state, GltfNode gltfNode, Node sceneNode)
        {
            if (sceneNode is VideoPlayer player)
            {
                var dict = new Dictionary();
                dict["video_node"] = state.GetNodeIndex(player.GetNodeOrNull(player.VideoPlayback));
                dict["audio_node"] = state.GetNodeIndex(player.GetNodeOrNull(player.AudioPlayback));
                gltfNode.SetAdditionalData(EXT_NAME, dict);
                state.AddUsedExtension(EXT_NAME, true);
            }
        }

        public override Error _ParseNodeExtensions(GltfState state, GltfNode gltfNode, Dictionary extensions)
        {
            if (extensions.TryGetDict(EXT_NAME, out Dictionary dict))
                gltfNode.SetAdditionalData(EXT_NAME, dict);
            return Error.Ok;
        }

        public override Error _ImportNode(GltfState state, GltfNode gltfNode, Dictionary json, Node node)
        {
            Variant val = gltfNode.GetAdditionalData(EXT_NAME);
            if (val.VariantType != Variant.Type.Dictionary)
                return Error.Ok;
            var data = val.AsGodotDictionary();
            if (node is VideoPlayer player)
            {
                if (data.TryGetInt32("video_node", out int video))
                    player.VideoPlayback = player.GetPathTo(state.GetSceneNode(video));
                if (data.TryGetInt32("audio_node", out int audio))
                    player.AudioPlayback = player.GetPathTo(state.GetSceneNode(audio));
            }

            return Error.Ok;
        }

        public override Node3D _GenerateSceneNode(GltfState state, GltfNode gltfNode, Node sceneParent)
        {
            Variant val = gltfNode.GetAdditionalData(EXT_NAME);
            if (val.VariantType != Variant.Type.Dictionary)
                return null;
            VideoPlayer player = new VideoPlayer();
            return player;
        }
    }
}
