using System;
using DitzelGames.FastIK;
using Godot;

public partial class IKSystem2 : Node
{
    public enum FootIkState : int
    {
        Idle,
        Moving,
        Timeout,
    }

    [Serializable]
    public struct IKData
    {
        public FastIKFabric2 ik;
        public Node3D target;
        public Node3D pole;
    }

    [ExportGroup("Settings")]
    [Export]
    public Node3D forwardNode;
    [Export]
    public Skeleton3D humanoid;
    [Export(PropertyHint.Range, "0,1")]
    public float SnapBackStrength = 0.5f;
    [Export]
    public bool handIk = true;
    [Export]
    public bool footIk = true;
    [Export]
    public bool moveFeet = true;

    [Export]
    public float minStepHeight = 0.1f;
    [Export]
    public float minStepDistance = 0.1f;
    [Export]
    public float maxStepDistance = 0.25f;
    [Export]
    public float footMoveSpeed = 1f;
    [Export]
    public Curve footAnimCurve;
    [Export]
    public float timeoutTime = 0.25f;

    // [ExportGroup("Debug area")]
    public IKData leftHandData;
    public IKData rightHandData;
    public IKData leftFootData;
    public IKData rightFootData;
    public Transform3D humanBindPose;

    [Export]
    public string head = "Head";
    public Node3D headTarget;
    public Vector3 direction;
    [Export]
    public string hips = "Hips";
    [Export]
    public string leftHand = "LeftHand";
    [Export]
    public string rightHand = "RightHand";
    [Export]
    public string leftUpperLeg = "LeftUpperLeg";
    [Export]
    public string rightUpperLeg = "RightUpperLeg";
    [Export]
    public string leftFoot = "LeftFoot";
    [Export]
    public string rightFoot = "RightFoot";

    public float footDistance;
    public float leftFootRest;
    public float rightFootRest;

    public Vector3 hipsPos;

    public Vector3 leftFootCurrentPos;
    public Vector3 leftFootSourcePos;
    public Vector3 leftFootTargetPos;
    public FootIkState leftState = FootIkState.Idle;
    public float leftTimer;
    public Vector3 rightFootCurrentPos;
    public Vector3 rightFootSourcePos;
    public Vector3 rightFootTargetPos;
    public FootIkState rightState = FootIkState.Idle;
    public float rightTimer;


    public override void _EnterTree()
    {
        // Init();
        RequestReady();
    }

    public override void _ExitTree()
    {
        OnDisable();
    }

    public override void _Ready()
    {
        // Init();
        CallDeferred(nameof(Init));
    }

    public override void _PhysicsProcess(double delta)
    {
        LateUpdate(delta);
    }

    private void OnEnable()
    {
        Init();
    }

    private static float MapValue(float value, float min1, float max1, float min2, float max2)
    {
        return (value - min1) / (max1 - min1) * (max2 - min2) + min2;
    }

    private static Vector3 DirectionToLocal(Transform3D t, Vector3 v)
    {
        return t.Basis.Inverse() * v;
    }
    
    public Transform3D GetBoneTransform(string bone)
    {
        return humanoid.GlobalTransform * humanoid.GetBoneGlobalPose(humanoid.FindBone(bone));
    }

    public Vector3 GetBonePosition(string bone)
    {
        return GetBoneTransform(bone).Origin;
    }

    public float GetScale()
    {
        return humanoid.GlobalBasis.Scale.Z;
    }

    private void LateUpdate(double delta)
    {
        if (!IsInstanceValid(humanoid))
            return;
        if (IsInstanceValid(leftHandData.ik))
            leftHandData.ik.Active = handIk;
        if (IsInstanceValid(rightHandData.ik))
            rightHandData.ik.Active = handIk;
        if (IsInstanceValid(leftFootData.ik))
            leftFootData.ik.Active = footIk;
        if (IsInstanceValid(rightFootData.ik))
            rightFootData.ik.Active = footIk;

        Vector3 vel = GetBonePosition(hips) - hipsPos;

        if (moveFeet)
        {
            // left
            switch (leftState)
            {
                default:
                case FootIkState.Idle:
                    if (leftFootData.ik.IsOutOfReach || IsFootMoveTime(PlaceLeftFoot(Vector3.Zero), leftFootCurrentPos, maxStepDistance))
                    {
                        leftFootSourcePos = leftFootCurrentPos;
                        leftFootTargetPos = PlaceLeftFoot(vel);
                        leftState = FootIkState.Moving;
                        leftTimer = 0f;
                    }
                    break;
                case FootIkState.Moving:
                    if (LerpFoot(ref leftFootCurrentPos, leftFootSourcePos, leftFootTargetPos, ref leftTimer, delta))
                    {
                        leftState = FootIkState.Timeout;
                        leftTimer = 0f;
                    }
                    break;
                case FootIkState.Timeout:
                    leftTimer += (float)delta;
                    if (leftTimer >= timeoutTime)
                    {
                        leftState = FootIkState.Idle;
                        leftTimer = 0f;
                    }
                    break;
            }
            leftFootData.target.GlobalPosition = leftFootCurrentPos;

            // right
            switch (rightState)
            {
                default:
                case FootIkState.Idle:
                    if (rightFootData.ik.IsOutOfReach || IsFootMoveTime(PlaceRightFoot(Vector3.Zero), rightFootCurrentPos, maxStepDistance))
                    {
                        rightFootSourcePos = rightFootCurrentPos;
                        rightFootTargetPos = PlaceRightFoot(vel);
                        rightState = FootIkState.Moving;
                        rightTimer = 0f;
                    }
                    break;
                case FootIkState.Moving:
                    if (leftState != FootIkState.Moving && LerpFoot(ref rightFootCurrentPos, rightFootSourcePos, rightFootTargetPos, ref rightTimer, delta))
                    {
                        rightState = FootIkState.Timeout;
                        rightTimer = 0f;
                    }
                    break;
                case FootIkState.Timeout:
                    rightTimer += (float)delta;
                    if (rightTimer >= timeoutTime)
                    {
                        rightState = FootIkState.Idle;
                        rightTimer = 0f;
                    }
                    break;
            }
            rightFootData.target.GlobalPosition = rightFootCurrentPos;
        }

        if (!string.IsNullOrEmpty(hips))
            hipsPos = GetBonePosition(hips);
    }

    public Vector3 PlaceLeftFoot(Vector3 vel)
    {
        return BaseToWorld(SnapBy2(WorldToBase(humanoid.GlobalPosition), minStepDistance)) - humanoid.GlobalBasis.X.Normalized() * footDistance * 0.5f + vel;
    }

    public Vector3 PlaceRightFoot(Vector3 vel)
    {
        return BaseToWorld(SnapBy2(WorldToBase(humanoid.GlobalPosition), minStepDistance)) + humanoid.GlobalBasis.X.Normalized() * footDistance * 0.5f + vel;
    }

    public static Vector2 SnapBy2(Vector2 input, float interval)
    {
        float x = Mathf.Round(input.X / interval) * interval;
        float y = Mathf.Round(input.Y / interval) * interval;
        return new Vector2(x, y);
    }

    public static Vector3 SnapBy3(Vector3 input, float interval)
    {
        float x = Mathf.Round(input.X / interval) * interval;
        float y = Mathf.Round(input.Y / interval) * interval;
        float z = Mathf.Round(input.Z / interval) * interval;
        return new Vector3(x, y, z);
    }

    public Vector2 WorldToBase(Vector3 input)
    {
        return new Vector2(input.X, input.Z);
    }

    public Vector3 BaseToWorld(Vector2 input)
    {
        return new Vector3(input.X, humanoid.GlobalPosition.Y, input.Y);
    }

    public bool CheckFootDot()
    {
        return Mathf.Abs((leftFootCurrentPos - humanoid.GlobalPosition).Dot(rightFootCurrentPos - humanoid.GlobalPosition)) > 0.25;
    }

    public bool IsFootMoveTime(Vector3 target, Vector3 current, float maxDist = 0.25f)
    {
        return (WorldToBase(target) - WorldToBase(current)).LengthSquared() > maxDist * maxDist;
        // return (new Vector2(foot.x, foot.z) - new Vector2(hips.x, hips.z)).sqrMagnitude < maxDist * maxDist;
    }

    public bool LerpFoot(ref Vector3 current, Vector3 source, Vector3 target, ref float t, double delta)
    {
        t += footMoveSpeed * (float)delta;
        Vector3 output = BaseToWorld(WorldToBase(source).Lerp(WorldToBase(target), t));
        output.Y += footAnimCurve.Sample(t) * minStepHeight;
        current = output;
        return t >= 1f;
    }

    public void Init()
    {
        leftTimer = 0f;
        rightTimer = 0f;
        OnDisable();
        if (!IsInstanceValid(forwardNode))
        {
            forwardNode = humanoid;
        }
        if (IsInstanceValid(humanoid))
        {
            humanBindPose = humanoid.Transform;
            direction = GetBonePosition(head) - GetBonePosition(hips);
            float scl = direction.Length() / GetScale();
            footDistance = GetBonePosition(leftFoot).DistanceTo(GetBonePosition(rightFoot));
            leftFootRest = GetBonePosition(hips).DistanceTo(GetBonePosition(leftFoot));
            rightFootRest = GetBonePosition(hips).DistanceTo(GetBonePosition(rightFoot));
            float scale = 1f / GetScale();
            float handPoleDist = -1f;

            Vector3 right = humanoid.GlobalBasis.X.Normalized();
            Vector3 forward = -humanoid.GlobalBasis.Z.Normalized();
            Vector3 up = humanoid.GlobalBasis.Y.Normalized();

            // hips and head
            {
                headTarget = new Node3D() { Name = "Head_Target" };
                humanoid.AddChild(headTarget);
                headTarget.GlobalTransform = GetBoneTransform(head);
                hipsPos = GetBonePosition(hips);
            }

            // left hand
            {
                leftHandData.target = new Node3D() { Name = "LeftHand_Target" };
                humanoid.AddChild(leftHandData.target);
                leftHandData.target.GlobalTransform = GetBoneTransform(leftHand);
                leftHandData.pole = new Node3D() { Name = "LeftHand_Pole" };
                // head.AddChild(leftHandData.pole);

                FastIKFabric2 ik = new FastIKFabric2();
                ik.AddChild(leftHandData.pole);
                ik.Active = false;
                ik.TargetBone = leftHand;
                ik.Target = leftHandData.target;
                ik.Pole = leftHandData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                humanoid.AddChild(ik);
                leftHandData.ik = ik;
                leftHandData.pole.GlobalPosition = GetBonePosition(head) - right * scl - forward * scl + up * handPoleDist * scl;
            }
            // right hand
            {
                rightHandData.target = new Node3D() { Name = "RightHand_Target" };
                humanoid.AddChild(rightHandData.target);
                rightHandData.target.GlobalTransform = GetBoneTransform(rightHand);
                rightHandData.pole = new Node3D() { Name = "RightHand_Pole" };
                // head.AddChild(rightHandData.pole);

                FastIKFabric2 ik = new FastIKFabric2();
                ik.AddChild(rightHandData.pole);
                ik.Active = false;
                ik.TargetBone = rightHand;
                ik.Target = rightHandData.target;
                ik.Pole = rightHandData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                humanoid.AddChild(ik);
                rightHandData.ik = ik;
                rightHandData.pole.GlobalPosition = GetBonePosition(head) + right * scl - forward * scl + up * handPoleDist * scl;
            }

            // left foot
            {
                leftFootData.target = new Node3D() { Name = "LeftFoot_Target" };
                humanoid.AddChild(leftFootData.target);
                leftFootData.target.GlobalTransform = GetBoneTransform(leftFoot);
                leftFootData.pole = new Node3D() { Name = "LeftFoot_Pole" };
                // hips.AddChild(leftFootData.pole);

                FastIKFabric2 ik = new FastIKFabric2();
                ik.AddChild(leftFootData.pole);
                ik.Active = false;
                ik.TargetBone = leftFoot;
                ik.Target = leftFootData.target;
                ik.Pole = leftFootData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                humanoid.AddChild(ik);
                leftFootData.ik = ik;
                leftFootData.pole.GlobalPosition = GetBonePosition(leftFoot) - right * 0f * scl + forward * 0.5f * scl + up * 0.5f * scl;
            }
            // right foot
            {
                rightFootData.target = new Node3D() { Name = "RightFoot_Target" };
                humanoid.AddChild(rightFootData.target);
                rightFootData.target.GlobalTransform = GetBoneTransform(rightFoot);
                rightFootData.pole = new Node3D() { Name = "RightFoot_Pole" };
                // hips.AddChild(rightFootData.pole);

                FastIKFabric2 ik = new FastIKFabric2();
                ik.AddChild(rightFootData.pole);
                ik.Active = false;
                ik.TargetBone = rightFoot;
                ik.Target = rightFootData.target;
                ik.Pole = rightFootData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                humanoid.AddChild(ik);
                rightFootData.ik = ik;
                rightFootData.pole.GlobalPosition = GetBonePosition(rightFoot) + right * 0f * scl + forward * 0.5f * scl + up * 0.5f * scl;
            }
        }
    }

    private void OnDisable()
    {
        void DestroyIKData(IKData data)
        {
            if (IsInstanceValid(data.target))
                data.target.QueueFree();
            if (IsInstanceValid(data.pole))
                data.pole.QueueFree();
            if (IsInstanceValid(data.ik))
                data.ik.QueueFree();
        }
        if (IsInstanceValid(headTarget))
            headTarget.QueueFree();
        DestroyIKData(leftHandData);
        DestroyIKData(rightHandData);
        DestroyIKData(leftFootData);
        DestroyIKData(rightFootData);
    }
}