using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.CCK;
using Hypernex.CCK.GodotVersion;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.Sandboxing;

namespace Hypernex.Game
{
    public partial class AvatarRoot : Node
    {
        public ISceneProvider safeLoader;
        public AvatarDescriptor descriptor;
        public IKSystem ikSystem;
        public Node3D target;
        public List<Node> Objects = new List<Node>();
        public List<ScriptRunner> Runners = new List<ScriptRunner>();

        public Node3D HeadTransform => ikSystem.headTarget;
        public Node3D LeftHandTransform => ikSystem.leftHandData.target;
        public Node3D RightHandTransform => ikSystem.rightHandData.target;

        public Transform3D eyesOffset;
        public Transform3D headOffset;

        public void AddObject(Node worldObject)
        {
            if (Objects.Contains(worldObject))
                return;
            if (worldObject is AvatarDescriptor desc)
            {
                descriptor = desc;
                if (IsInstanceValid(desc.GetEyes()))
                    eyesOffset = desc.GetEyes().Transform;
                headOffset = desc.GetSkeleton().GetBoneGlobalRest(desc.GetSkeleton().FindBone("Head"));
            }
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

        public async void InitIk()
        {
            if (IsInstanceValid(ikSystem))
                ikSystem.QueueFree();
            ikSystem = new IKSystem();
            // ikSystem.forwardNode = target;
            ikSystem.humanoid = descriptor.GetSkeleton();
            ikSystem.SnapBackStrength = 0f;
            ikSystem.minStepHeight = 0f;
            ikSystem.maxStepHeight = 0.4f;
            ikSystem.minStepLength = -0.4f;
            ikSystem.maxStepLength = 0.4f;
            CreateBoneTree();
            ikSystem.head = FindBone("Head");
            ikSystem.hips = FindBone("Hips");
            ikSystem.leftHand = FindBone("LeftHand");
            ikSystem.rightHand = FindBone("RightHand");
            ikSystem.leftUpperLeg = FindBone("LeftUpperLeg");
            ikSystem.rightUpperLeg = FindBone("RightUpperLeg");
            ikSystem.leftFoot = FindBone("LeftFoot");
            ikSystem.rightFoot = FindBone("RightFoot");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            AddChild(ikSystem);
        }

        private void CalcIk()
        {
            ikSystem.leftHandData.ik.ResolveIK();
            ikSystem.rightHandData.ik.ResolveIK();
            ikSystem.leftFootData.ik.ResolveIK();
            ikSystem.rightFootData.ik.ResolveIK();
        }

        public void ProcessIk(bool vr, bool useHead, Transform3D head, Transform3D floor, Transform3D leftHand, Transform3D rightHand)
        {
            if (IsInstanceValid(ikSystem) && ikSystem.IsNodeReady())
            {
                Node3D root = ikSystem.humanoid;

                if (useHead)
                {
                    ikSystem.humanoid.GlobalPosition = floor.Origin;
                    Vector3 hipRotation = floor.Basis.GetEuler();
                    hipRotation.X = 0f;
                    hipRotation.Y += Mathf.Pi;
                    hipRotation.Z = 0f;
                    ikSystem.humanoid.GlobalRotation = hipRotation;
                    Vector3 headScl = ikSystem.headTarget.Scale;
                    ikSystem.headTarget.GlobalTransform = head.RotatedLocal(Vector3.Up, Mathf.Pi);
                    ikSystem.headTarget.Scale = headScl;
                    ikSystem.hips.GlobalPosition = ikSystem.headTarget.GlobalPosition + (Vector3.Down * ikSystem.hipsDistance);
                }
                else
                {
                    Vector3 scale = root.Scale;
                    root.GlobalPosition = target.GlobalPosition;
                    root.GlobalRotation = target.GlobalRotation + new Vector3(0f, Mathf.Pi, 0f);
                    root.Scale = scale;
                    ikSystem.hips.GlobalPosition = ikSystem.headTarget.GlobalPosition + (Vector3.Down * ikSystem.hipsDistance);
                }

                if (!vr)
                {
                    CalcIk();
                    return;
                }

                Vector3 leftScl = ikSystem.leftHandData.target.Scale;
                ikSystem.leftHandData.target.GlobalTransform = leftHand.RotatedLocal(Vector3.Up, Mathf.Pi).RotatedLocal(Vector3.Right, Mathf.Pi * 0.5f);//.RotatedLocal(Vector3.Up, Mathf.Pi * -0.5f);
                ikSystem.leftHandData.target.Scale = leftScl;

                Vector3 rightScl = ikSystem.rightHandData.target.Scale;
                ikSystem.rightHandData.target.GlobalTransform = rightHand.RotatedLocal(Vector3.Up, Mathf.Pi).RotatedLocal(Vector3.Right, Mathf.Pi * 0.5f);//.RotatedLocal(Vector3.Up, Mathf.Pi * 0.5f);
                ikSystem.rightHandData.target.Scale = rightScl;

                CalcIk();
            }
        }

        public void ResetPose(BoneAttachment3D bone)
        {
            bone.Transform = descriptor.GetSkeleton().GetBoneGlobalRest(bone.BoneIdx);
        }

        public void ResetPose(string bone)
        {
            foreach (var ch in descriptor.GetSkeleton().GetChildren())
            {
                if (ch is BoneAttachment3D boneAttachment)
                {
                    if (boneAttachment.BoneName == bone)
                    {
                        ResetPose(boneAttachment);
                        return;
                    }
                }
            }
        }

        public void CreateBoneTree(int idx)
        {
            Skeleton3D skeleton = descriptor.GetSkeleton();
            int parent = skeleton.GetBoneParent(idx);
            BoneAttachment3D bone = SetupBone(idx);
            if (parent == -1)
                skeleton.AddChild(bone);
            else
                FindBone(parent).AddChild(bone);
            bone.SetExternalSkeleton(bone.GetPathTo(skeleton));
            int[] children = skeleton.GetBoneChildren(idx);
            foreach (var ch in children)
            {
                CreateBoneTree(ch);
            }
        }

        public void CreateBoneTree()
        {
            Skeleton3D skeleton = descriptor.GetSkeleton();
            List<int> bones = skeleton.GetParentlessBones().ToList();
            foreach (var idx in bones)
            {
                CreateBoneTree(idx);
            }
        }

        public BoneAttachment3D SetupBone(int idx)
        {
            Skeleton3D skeleton = descriptor.GetSkeleton();
            BoneAttachment3D final = new BoneAttachment3D();
            final.SetUseExternalSkeleton(true);
            final.Name = skeleton.GetBoneName(idx);
            final.BoneIdx = idx;
            final.OverridePose = true;
            final.Transform = descriptor.GetSkeleton().GetBoneRest(idx);
            return final;
        }

        public Node3D FindBone(int idx)
        {
            foreach (var ch in descriptor.GetSkeleton().FindChildren("*", owned: false))
            {
                if (ch is BoneAttachment3D boneAttachment)
                {
                    if (boneAttachment.BoneIdx == idx || (!string.IsNullOrEmpty(boneAttachment.BoneName) && boneAttachment.BoneName == descriptor.GetSkeleton().GetBoneName(idx)))
                        return boneAttachment;
                }
            }
            return null;
        }

        public BoneAttachment3D FindBone(string bone)
        {
            foreach (var ch in descriptor.GetSkeleton().FindChildren("*", owned: false))
            {
                if (ch is BoneAttachment3D boneAttachment)
                {
                    if (boneAttachment.BoneName == bone || (boneAttachment.BoneIdx != -1 && boneAttachment.BoneIdx == descriptor.GetSkeleton().FindBone(bone)))
                    {
                        return boneAttachment;
                    }
                }
            }
            return null;
        }

        public override void _ExitTree()
        {
            safeLoader.Dispose();
        }

        public static AvatarRoot LoadFromFile(string path)
        {
            AvatarRoot root = new AvatarRoot();
            ISceneProvider loader = Init.AvatarProvider();
            root.safeLoader = loader;
            PackedScene scn = null;
            try
            {
                scn = loader.LoadFromFile(path);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
            }
            if (!IsInstanceValid(scn))
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
