using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Hypernex.CCK.GodotVersion.Classes;

namespace Hypernex.CCK.GodotVersion.Extensions
{
    [Tool]
    [GlobalClass]
    public partial class GltfAvatarExt : GltfDocumentExtension
    {
        public const string EXT_NAME = "HYPERNEX_avatar";
        public const string EXTS = "extensions";

        public override string[] _GetSupportedExtensions()
        {
            return new string[] { EXT_NAME };
        }

        public override Error _ImportPreflight(GltfState state, string[] extensions)
        {
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

        public override void _ConvertSceneNode(GltfState state, GltfNode gltfNode, Node sceneNode)
        {
            if (sceneNode is AvatarDescriptor avatar)
            {
                var dict = new Dictionary();

                int skel = -1;
                var skeleton = avatar.GetSkeleton();
                var skeletons = state.GetSkeletons();
                for (int i = 0; i < skeletons.Count; i++)
                {
                    if (skeletons[i].GetGodotSkeleton() == skeleton)
                    {
                        skel = i;
                        break;
                    }
                }
                dict["skeleton"] = skel;
                var eyes = avatar.GetEyes();
                if (IsInstanceValid(eyes))
                    dict["eyes"] = state.GetNodeIndex(eyes);
                else
                    dict["eyes"] = -1;

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
            if (node is AvatarDescriptor desc)
            {
                if (data.TryGetInt32("skeleton", out int skel))
                {
                    var skels = state.GetSkeletons();
                    if (skel >= 0 && skel < skels.Count)
                        desc.Skeleton = desc.GetPathTo(skels[skel].GetGodotSkeleton());
                }
                if (data.TryGetInt32("eyes", out int eyes))
                {
                    var nodes = state.GetNodes();
                    if (eyes >= 0 && eyes < nodes.Count)
                        desc.Eyes = desc.GetPathTo(state.GetSceneNode(eyes));
                }
            }
            return Error.Ok;
        }

        public override Node3D _GenerateSceneNode(GltfState state, GltfNode gltfNode, Node sceneParent)
        {
            Variant val = gltfNode.GetAdditionalData(EXT_NAME);
            if (val.VariantType != Variant.Type.Dictionary)
                return null;
            // var data = val.AsGodotDictionary();
            AvatarDescriptor avatar = new AvatarDescriptor();

            return avatar;
        }
    }
}
