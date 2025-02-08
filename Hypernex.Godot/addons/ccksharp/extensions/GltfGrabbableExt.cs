using System;
using Godot;
using Godot.Collections;
using Hypernex.CCK.GodotVersion.Classes;

namespace Hypernex.CCK.GodotVersion.Extensions
{
    [Tool]
    [GlobalClass]
    public partial class GltfGrabbableExt : GltfDocumentExtension
    {
        public const string EXT_NAME = "HYPERNEX_grabbable";
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
            if (sceneNode is GrabbableDescriptor grab)
            {
                var dict = new Dictionary();
                dict["apply_velocity"] = grab.ApplyVelocity;
                dict["velocity_multiplier"] = grab.VelocityAmount;
                dict["min_velocity"] = grab.VelocityThreshold;
                dict["laser_grab"] = grab.GrabByLaser;
                dict["laser_max_distance"] = grab.LaserGrabDistance;
                dict["distance_grab"] = grab.GrabByDistance;
                dict["grab_max_distance"] = grab.GrabDistance;
                gltfNode.SetAdditionalData(EXT_NAME, dict);
                state.AddUsedExtension(EXT_NAME, false);
            }
        }

        public override Error _ParseNodeExtensions(GltfState state, GltfNode gltfNode, Dictionary extensions)
        {
            if (extensions.TryGetDict(EXT_NAME, out Dictionary dict))
                gltfNode.SetAdditionalData(EXT_NAME, dict);
            return Error.Ok;
        }

        public override Node3D _GenerateSceneNode(GltfState state, GltfNode gltfNode, Node sceneParent)
        {
            Variant val = gltfNode.GetAdditionalData(EXT_NAME);
            if (val.VariantType != Variant.Type.Dictionary)
                return null;
            var data = val.AsGodotDictionary();
            GrabbableDescriptor grab = new GrabbableDescriptor();
            if (data.TryGetBool("apply_velocity", out bool applyVel))
                grab.ApplyVelocity = applyVel;
            if (data.TryGetFloat("velocity_multiplier", out float velMulti))
                grab.VelocityAmount = velMulti;
            if (data.TryGetFloat("min_velocity", out float velMin))
                grab.VelocityThreshold = velMin;
            if (data.TryGetBool("laser_grab", out bool laser))
                grab.GrabByLaser = laser;
            if (data.TryGetFloat("laser_max_distance", out float laserDist))
                grab.LaserGrabDistance = laserDist;
            if (data.TryGetBool("distance_grab", out bool distGrab))
                grab.GrabByDistance = distGrab;
            if (data.TryGetFloat("grab_max_distance", out float grabDist))
                grab.GrabDistance = grabDist;
            return grab;
        }
    }
}
