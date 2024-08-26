using System;
using Godot;

namespace DitzelGames.FastIK
{
    /// <summary>
    /// Fabrik IK Solver
    /// </summary>
    [GlobalClass]
    public partial class FastIKFabric : Node
    {
        public class BoneData
        {
            public Node3D Node { get; set; }
            public int BoneIndex { get; set; }
            public Transform3D GlobalTransform
            {
                get
                {
                    if (!IsInstanceValid(Node))
                        return default;
                    // return Node.GlobalTransform * Transform;
                    if (Node is Skeleton3D skeleton)
                    {
                        return skeleton.GlobalTransform * Transform;
                    }
                    return Node.GlobalTransform;
                }
                set
                {
                    if (!IsInstanceValid(Node))
                        return;
                    if (Node is Skeleton3D skeleton)
                    {
                        Transform = skeleton.GlobalTransform.AffineInverse() * value;
                    }
                    else
                        Node.GlobalTransform = value;
                }
            }
            public Transform3D Transform
            {
                get
                {
                    if (!IsInstanceValid(Node))
                        return default;
                    if (Node is Skeleton3D skeleton)
                    {
                        return skeleton.GetBoneGlobalPose(BoneIndex);
                    }
                    return Node.Transform;
                }
                set
                {
                    if (!IsInstanceValid(Node))
                        return;
                    if (Node is Skeleton3D skeleton)
                    {
                        skeleton.SetBoneGlobalPoseOverride(BoneIndex, value, 1f, true);
                    }
                    else
                        Node.Transform = value;
                }
            }
            public Vector3 GlobalPosition
            {
                get => GlobalTransform.Origin;
                set
                {
                    var t = GlobalTransform;
                    t.Origin = value;
                    GlobalTransform = t;
                }
            }
            public Vector3 GlobalRotation
            {
                get => GlobalTransform.Basis.Orthonormalized().GetEuler();
                set
                {
                    var t = GlobalTransform;
                    t.Basis = Basis.FromEuler(value).Scaled(GlobalTransform.Basis.Scale).Orthonormalized();
                    GlobalTransform = t;
                }
            }
            public Quaternion GlobalQuaternion
            {
                get => GlobalTransform.Basis.Orthonormalized().GetRotationQuaternion();
                set
                {
                    var t = GlobalTransform;
                    t.Basis = new Basis(value).Scaled(GlobalTransform.Basis.Scale).Orthonormalized();
                    GlobalTransform = t;
                }
            }

            public BoneData(Skeleton3D skeleton, int idx)
            {
                throw new Exception();
                Node = skeleton;
                BoneIndex = idx;
            }

            public BoneData(Node3D node)
            {
                Node = node;
                if (node is BoneAttachment3D boneAttachment)
                {
                    Skeleton3D skeleton = boneAttachment.GetUseExternalSkeleton() ? boneAttachment.GetNode<Skeleton3D>(boneAttachment.GetExternalSkeleton()) : boneAttachment.GetParent<Skeleton3D>();
                    // Node = skeleton;
                    BoneIndex = boneAttachment.BoneIdx;
                }
                else
                    BoneIndex = -1;
            }

            public BoneData GetParent()
            {
                if (!IsInstanceValid(Node))
                    return null;
                /*
                if (Node is BoneAttachment3D boneAttachment)
                {
                    Skeleton3D skeleton = boneAttachment.GetUseExternalSkeleton() ? boneAttachment.GetNode<Skeleton3D>(boneAttachment.GetExternalSkeleton()) : boneAttachment.GetParent<Skeleton3D>();
                    int idx = skeleton.GetBoneParent(boneAttachment.BoneIdx);
                    if (idx == -1)
                        return null;
                    // /*
                    foreach (var ch in skeleton.GetChildren())
                    {
                        if (ch is BoneAttachment3D b)
                        {
                            if (b.BoneIdx == idx)
                            {
                                // boneAttachment.Reparent(b);
                                return new BoneData(b);
                            }
                        }
                    }
                    return new BoneData(Node.GetParentNode3D());
                    BoneAttachment3D a = new BoneAttachment3D()
                    {
                        BoneIdx = idx,
                        OverridePose = true,
                        Transform = skeleton.GetBoneGlobalRest(idx),
                    };
                    skeleton.AddChild(a);
                    // boneAttachment.Reparent(a);
                    return new BoneData(a);
                    // *
                    return new BoneData(skeleton, idx);
                }
                else if (Node is Skeleton3D skeleton)
                {
                    int idx = skeleton.GetBoneParent(BoneIndex);
                    if (idx == -1)
                        return null;
                    return new BoneData(skeleton, idx);
                }
                */
                return new BoneData(Node.GetParentNode3D());
            }
        }

        [Export]
        public bool Enabled = true;

        /// <summary>
        /// Chain length of bones
        /// </summary>
        [Export]
        public int ChainLength = 2;

        /// <summary>
        /// Target the chain should bent to
        /// </summary>
        [Export]
        public Node3D Target;
        [Export]
        public Node3D Pole;

        /// <summary>
        /// Solver iterations per update
        /// </summary>
        [ExportGroup("Solver Parameters")]
        [Export]
        public int Iterations = 10;

        /// <summary>
        /// Distance when the solver stops
        /// </summary>
        [Export]
        public float Delta = 0.001f;

        /// <summary>
        /// Strength of going back to the start position.
        /// </summary>
        // [Range(0, 1)]
        [Export(PropertyHint.Range, "0,1")]
        public float SnapBackStrength = 1f;


        protected float[] BonesLength; //Target to Origin
        public float CompleteLength { get; protected set; }
        public BoneData[] Bones { get; protected set; }
        protected Vector3[] Positions;
        protected Vector3[] StartDirectionSucc;
        protected Quaternion[] StartRotationBone;
        protected Quaternion StartRotationTarget;
        protected BoneData Root;

        public bool IsOutOfReach { get; protected set; } = false;
        public float ReachAmountSqr { get; protected set; } = 0f;


        public override void _Ready()
        {
            Init();
        }

        public void Init()
        {
            //initial array
            Bones = new BoneData[ChainLength + 1];
            Positions = new Vector3[ChainLength + 1];
            BonesLength = new float[ChainLength];
            StartDirectionSucc = new Vector3[ChainLength + 1];
            StartRotationBone = new Quaternion[ChainLength + 1];

            //find root
            Root = new BoneData(GetParent<Node3D>());
            for (var i = 0; i <= ChainLength; i++)
            {
                if (Root == null)
                    throw new Exception("The chain value is longer than the ancestor chain!");
                Root = Root.GetParent();
            }

            //init target
            if (!IsInstanceValid(Target))
            {
                return;
            }
            StartRotationTarget = GetRotationRootSpace(Target);


            //init data
            var current = new BoneData(GetParent<Node3D>());
            CompleteLength = 0;
            for (var i = Bones.Length - 1; i >= 0; i--)
            {
                Bones[i] = current;
                StartRotationBone[i] = GetRotationRootSpace(current);

                if (i == Bones.Length - 1)
                {
                    //leaf
                    StartDirectionSucc[i] = GetPositionRootSpace(Target) - GetPositionRootSpace(current);
                }
                else
                {
                    //mid bone
                    StartDirectionSucc[i] = GetPositionRootSpace(Bones[i + 1]) - GetPositionRootSpace(current);
                    BonesLength[i] = StartDirectionSucc[i].Length();
                    CompleteLength += BonesLength[i];
                }

                current = current.GetParent();
            }



        }

        public override void _Process(double delta)
        {
            if (!Enabled)
                return;
            ResolveIK();
        }

        public void ResolveIK()
        {
            if (!IsInstanceValid(Target))
                return;

            if (BonesLength.Length != ChainLength)
                Init();
            
            //Fabric

            //  root
            //  (bone0) (bonelen 0) (bone1) (bonelen 1) (bone2)...
            //   x--------------------x--------------------x---...

            //get position
            for (int i = 0; i < Bones.Length; i++)
                Positions[i] = GetPositionRootSpace(Bones[i]);

            var targetPosition = GetPositionRootSpace(Target);
            var targetRotation = GetRotationRootSpace(Target);

            //1st is possible to reach?
            ReachAmountSqr = (targetPosition - GetPositionRootSpace(Bones[0])).LengthSquared();
            if (ReachAmountSqr >= CompleteLength * CompleteLength)
            {
                IsOutOfReach = true;
                //just strech it
                var direction = (targetPosition - Positions[0]).Normalized();
                //set everything after root
                for (int i = 1; i < Positions.Length; i++)
                    Positions[i] = Positions[i - 1] + direction * BonesLength[i - 1];
            }
            else
            {
                IsOutOfReach = false;
                for (int i = 0; i < Positions.Length - 1; i++)
                    Positions[i + 1] = Positions[i + 1].Lerp(Positions[i] + StartDirectionSucc[i], SnapBackStrength);

                for (int iteration = 0; iteration < Iterations; iteration++)
                {
                    //https://www.youtube.com/watch?v=UNoX65PRehA
                    //back
                    for (int i = Positions.Length - 1; i > 0; i--)
                    {
                        if (i == Positions.Length - 1)
                            Positions[i] = targetPosition; //set it to target
                        else
                            Positions[i] = Positions[i + 1] + (Positions[i] - Positions[i + 1]).Normalized() * BonesLength[i]; //set in line on distance
                    }

                    //forward
                    for (int i = 1; i < Positions.Length; i++)
                        Positions[i] = Positions[i - 1] + (Positions[i] - Positions[i - 1]).Normalized() * BonesLength[i - 1];

                    //close enough?
                    if ((Positions[Positions.Length - 1] - targetPosition).LengthSquared() < Delta * Delta)
                        break;
                }
            }

            //move towards pole
            if (IsInstanceValid(Pole))
            {
                var polePosition = GetPositionRootSpace(Pole);
                for (int i = 1; i < Positions.Length - 1; i++)
                {
                    var plane = new Plane(Positions[i + 1] - Positions[i - 1], Positions[i - 1]);
                    var projectedPole = plane.Project(polePosition); // TODO: does this work?
                    var projectedBone = plane.Project(Positions[i]); // TODO: does this work?
                    var angle = (projectedBone - Positions[i - 1]).SignedAngleTo(projectedPole - Positions[i - 1], plane.Normal);
                    Positions[i] = new Quaternion(plane.Normal.Normalized(), angle) * (Positions[i] - Positions[i - 1]) + Positions[i - 1];
                }
            }

            //set position & rotation
            for (int i = 0; i < Positions.Length; i++)
            {
                if (i == Positions.Length - 1)
                    SetRotationRootSpace(Bones[i], targetRotation.Inverse() * StartRotationTarget * StartRotationBone[i].Inverse());
                else
                    SetRotationRootSpace(Bones[i], new Quaternion(StartDirectionSucc[i].Normalized(), (Positions[i + 1] - Positions[i]).Normalized()) * StartRotationBone[i].Inverse());
                SetPositionRootSpace(Bones[i], Positions[i]);
            }
        }

        private Vector3 GetPositionRootSpace(Node3D current)
        {
            if (Root == null)
                return current.GlobalPosition;
            else
                return Root.GlobalQuaternion.Inverse() * (current.GlobalPosition - Root.GlobalPosition);
        }

        private Quaternion GetRotationRootSpace(Node3D current)
        {
            //inverse(after) * before => rot: before -> after
            if (Root == null)
                return current.GlobalBasis.GetRotationQuaternion().Normalized();
            else
                return current.GlobalBasis.GetRotationQuaternion().Normalized().Inverse() * Root.GlobalQuaternion;
        }

        private Vector3 GetPositionRootSpace(BoneData current)
        {
            if (Root == null)
                return current.GlobalPosition;
            else
                return Root.GlobalQuaternion.Inverse() * (current.GlobalPosition - Root.GlobalPosition);
        }

        private void SetPositionRootSpace(BoneData current, Vector3 position)
        {
            if (Root == null)
                current.GlobalPosition = position;
            else
                current.GlobalPosition = Root.GlobalQuaternion * position + Root.GlobalPosition;
        }

        private Quaternion GetRotationRootSpace(BoneData current)
        {
            //inverse(after) * before => rot: before -> after
            if (Root == null)
                return current.GlobalQuaternion;
            else
                return current.GlobalQuaternion.Inverse() * Root.GlobalQuaternion;
        }

        private void SetRotationRootSpace(BoneData current, Quaternion rotation)
        {
            if (Root == null)
                current.GlobalQuaternion = rotation;
            else
                current.GlobalQuaternion = Root.GlobalQuaternion * rotation;
        }
    }
}