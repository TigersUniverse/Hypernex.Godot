using System;
using System.Collections.Generic;
using Godot;
using Hypernex.CCK;
using Hypernex.CCK.GodotVersion;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.Sandboxing;

namespace Hypernex.Game
{
    public partial class AvatarRoot : Node
    {
        public AvatarDescriptor descriptor;
        public IKSystem ikSystem;
        public Node3D target;
        public List<Node> Objects = new List<Node>();
        public List<ScriptRunner> Runners = new List<ScriptRunner>();

        public override void _PhysicsProcess(double delta)
        {
            Vector3 scale = descriptor.GetSkeleton().Scale;
            descriptor.GetSkeleton().GlobalPosition = target.GlobalPosition;
            descriptor.GetSkeleton().GlobalRotation = target.GlobalRotation + new Vector3(0f, Mathf.Pi, 0f);
            descriptor.GetSkeleton().Scale = scale;
        }

        public void AddObject(Node worldObject)
        {
            if (Objects.Contains(worldObject))
                return;
            if (worldObject is AvatarDescriptor desc)
                descriptor = desc;
            if (worldObject is AudioStreamPlayer audio)
            {
                audio.Bus = "Avatar";
            }
            if (worldObject is AudioStreamPlayer2D audio2d)
            {
                audio2d.Bus = "Avatar";
            }
            if (worldObject is AudioStreamPlayer3D audio3d)
            {
                audio3d.Bus = "Avatar";
            }
            worldObject.Owner = this;
            Objects.Add(worldObject);
            foreach (var child in worldObject.GetChildren())
            {
                AddObject(child);
            }
        }

        public void AttachTo(Node3D to)
        {
            target = to;
            InitIk();
        }

        public void InitIk()
        {
            if (IsInstanceValid(ikSystem))
                ikSystem.QueueFree();
            ikSystem = new IKSystem();
            // ikSystem.forwardNode = target;
            ikSystem.humanoid = descriptor.GetSkeleton();
            ikSystem.SnapBackStrength = 1f;
            ikSystem.minStepHeight = 0f;
            ikSystem.maxStepHeight = 0.4f;
            ikSystem.minStepLength = -0.4f;
            ikSystem.maxStepLength = 0.4f;
            ikSystem.head = GetBone("Head");
            ikSystem.hips = GetBone("Hips");
            ikSystem.leftHand = GetBone("LeftHand");
            ikSystem.rightHand = GetBone("RightHand");
            ikSystem.leftUpperLeg = GetBone("LeftUpperLeg");
            ikSystem.rightUpperLeg = GetBone("RightUpperLeg");
            ikSystem.leftFoot = GetBone("LeftFoot");
            ikSystem.rightFoot = GetBone("RightFoot");
            AddChild(ikSystem);
        }

        public BoneAttachment3D GetBone(string bone)
        {
            foreach (var ch in descriptor.GetSkeleton().GetChildren())
            {
                if (ch is BoneAttachment3D boneAttachment)
                {
                    if (boneAttachment.BoneName == bone)
                        return boneAttachment;
                }
            }
            BoneAttachment3D final = new BoneAttachment3D();
            final.Name = bone;
            final.BoneName = bone;
            final.OverridePose = true;
            final.Transform = descriptor.GetSkeleton().GetBoneGlobalPose(descriptor.GetSkeleton().FindBone(bone));
            descriptor.GetSkeleton().AddChild(final);
            return final;
        }

        public static AvatarRoot LoadFromFile(string path)
        {
            AvatarRoot root = new AvatarRoot();
            SafeLoader loader = new SafeLoader();
            loader.validScripts.Add(AvatarDescriptor.TypeName, SafeLoader.LoadScript<AvatarDescriptor>());
            try
            {
                loader.ReadZip(path);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
            }
            PackedScene scn = loader.scene;
            if (!IsInstanceValid(loader.scene))
            {
                Logger.CurrentLogger.Error("Unable to load avatar!");
                return root;
            }
            Node node = scn.Instantiate();
            if (IsInstanceValid(node))
            {
                root.AddChild(node);
                root.AddObject(node);
            }
            return root;
        }
    }
}
