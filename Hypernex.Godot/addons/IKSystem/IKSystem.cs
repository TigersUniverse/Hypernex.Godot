#if true
using System;
using DitzelGames.FastIK;
using Godot;

public partial class IKSystem : Node
{
    public enum IKLoopState
    {
        Apex,
        Buildup,
        Contact,
        FollowThru,
        ThruApex,
        Max,
    }

    [Serializable]
    public struct IKData
    {
        public FastIKFabric ik;
        public Node3D target;
        public Node3D pole;
    }

    [ExportGroup("Settings")]
    [Export]
    public Node3D forwardNode;
    [Export]
    public Skeleton3D humanoid;
    // [Range(0f, 1f)]
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
    public float maxStepHeight = 0.2f;

    [Export]
    public float minStepLength = -0.5f;
    [Export]
    public float maxStepLength = 0.5f;

    [Export]
    public float loopSize = 1f;

    // [Min(0f)]
    [Export]
    public float stepDistance = 0.2f;
    // [Min(0f)]
    [Export]
    public float reachDistance = 0.2f;
    // [Min(0.01f)]
    [Export]
    public float footMoveSpeed = 1f;
    [Export]
    public Curve velCurve;

    // [ExportGroup("Debug area")]
    public IKData leftHandData;
    public IKData rightHandData;
    public IKData leftFootData;
    public IKData rightFootData;
    public Transform3D humanBindPose;

    [Export]
    public BoneAttachment3D head;
    public Node3D headTarget;
    public Vector3 direction;
    [Export]
    public BoneAttachment3D hips;
    [Export]
    public BoneAttachment3D leftHand;
    [Export]
    public BoneAttachment3D rightHand;
    [Export]
    public BoneAttachment3D leftUpperLeg;
    [Export]
    public BoneAttachment3D rightUpperLeg;
    [Export]
    public BoneAttachment3D leftFoot;
    [Export]
    public BoneAttachment3D rightFoot;

    public float footDistance;
    public float hipsDistance;
    public float floorDistance;

    public Vector3 leftFootPos;
    public Vector3 rightFootPos;
    public bool flipFlop = false;
    public float flipDist => flipFlop ? 1f : 1f;

    public Vector3 hipsPos;
    public Vector3 lastVel;
    public float speedAvg;
    public float timer;
    public float velTimer;
    public float leftTimer;
    public float rightTimer;

    public IKLoopState leftLoopState;
    public IKLoopState rightLoopState;

    public override void _EnterTree()
    {
        Init();
    }

    public override void _ExitTree()
    {
        OnDisable();
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

    public float GetScale()
    {
        return humanoid.GlobalBasis.Scale.Z;
    }

    public void SetPose(BoneAttachment3D bone, IKData data)
    {
        var xform = data.target.Transform;
        var scl = data.target.Scale;
        data.target.Transform *= humanoid.GetBoneRest(bone.BoneIdx).AffineInverse();
        data.target.Scale = scl;
        data.ik.ResolveIK();
        data.target.Transform = xform;
    }

    public void CalcIk()
    {
        SetPose(leftHand, leftHandData);
        SetPose(rightHand, rightHandData);
        leftFootData.ik.ResolveIK();
        rightFootData.ik.ResolveIK();
        // SetPose(leftFoot, leftFootData);
        // SetPose(rightFoot, rightFootData);
    }

    private void LateUpdate(double delta)
    {
        if (!IsInstanceValid(humanoid))
            return;
        if (IsInstanceValid(leftHandData.ik))
            leftHandData.ik.Enabled = handIk;
        if (IsInstanceValid(rightHandData.ik))
            rightHandData.ik.Enabled = handIk;
        if (IsInstanceValid(leftFootData.ik))
            leftFootData.ik.Enabled = footIk;
        if (IsInstanceValid(rightFootData.ik))
            rightFootData.ik.Enabled = footIk;
        if (IsInstanceValid(hips) && IsInstanceValid(head) && IsInstanceValid(headTarget))
        {
            // hips.Position = hips.GetParentNode3D().ToLocal(headTarget.GlobalPosition) - hips.GetParentNode3D().ToLocal(direction);
            // hips.Position = hips.GetParentNode3D().InverseTransformPoint(headTarget.GlobalPosition) - hips.GetParentNode3D().InverseTransformVector(direction);
            Vector3 scl = head.Scale;
            head.GlobalPosition = headTarget.GlobalPosition;
            head.GlobalBasis = headTarget.GlobalBasis;
            head.Scale = scl;
        }

        Vector3 vel = hips.GlobalPosition - hipsPos;

        if (moveFeet)
        {
            float speed = lastVel.Length();
            // speed = 1f;
            float scale = 1f / GetScale();
            float unitScaleHeight = scale * hipsDistance;
            float unitScaleLength = scale;
            float scaleLength = lastVel.Length() / (float)delta;
            scaleLength = Mathf.Min(scaleLength, footDistance * 2f);
            // scaleLength = 1f;
            {
                Vector2 pos = EvaluateLoop(ref leftLoopState, ref leftTimer, loopSize, speed, 0f, true);
                float y = MapValue(pos.Y, 0f, 1f, minStepHeight * unitScaleHeight, maxStepHeight * unitScaleHeight) * scaleLength;
                float z = MapValue(pos.X, -1f, 1f, minStepLength * unitScaleLength, maxStepLength * unitScaleLength) * scaleLength;
                float x = -footDistance * 0.5f * scale;

                Vector3 end = Vector3.Zero;
                float t = 0f;
                if (lastVel.Length() > 0f)
                {
                    var dir = DirectionToLocal(forwardNode.GlobalTransform, lastVel.Normalized()).Normalized();
                    t = Mathf.Atan2(dir.X, dir.Z);
                    end += new Vector3(0f, y, z).Rotated(Vector3.Up, t);
                }
                // else
                //     leftTimer = 0f;
                end += new Vector3(x, 0f, 0f);
                leftFootData.target.Position = end;
            }
            {
                if (rightLoopState == leftLoopState)
                    rightLoopState = (IKLoopState)(((int)rightLoopState + 1) % (int)IKLoopState.Max);
                // rightLoopState = (IKLoopState)(((int)leftLoopState + 2) % (int)IKLoopState.Max);
                // rightTimer = leftTimer;

                Vector2 pos = EvaluateLoop(ref rightLoopState, ref rightTimer, loopSize, speed, 0.5f, true);
                float y = MapValue(pos.Y, 0f, 1f, minStepHeight * unitScaleHeight, maxStepHeight * unitScaleHeight) * scaleLength;
                float z = MapValue(pos.X, -1f, 1f, minStepLength * unitScaleLength, maxStepLength * unitScaleLength) * scaleLength;
                float x = footDistance * 0.5f * scale;

                Vector3 end = Vector3.Zero;
                float t = 0f;
                if (lastVel.Length() > 0f)
                {
                    var dir = DirectionToLocal(forwardNode.GlobalTransform, lastVel.Normalized()).Normalized();
                    t = Mathf.Atan2(dir.X, dir.Z);
                    end += new Vector3(0f, y, z).Rotated(Vector3.Up, t);
                }
                // else
                //     rightTimer = 0f;
                end += new Vector3(x, 0f, 0f);
                rightFootData.target.Position = end;
            }
        }

        if (IsInstanceValid(hips))
        {
            hipsPos = hips.GlobalPosition;
            // if (vel.Length() > 0f)
            {
                Vector3 newvel = vel;
                newvel.Y = 0f;
                lastVel = lastVel.Lerp(newvel, (float)delta * 10f);
            }
            speedAvg += vel.Length();
            speedAvg *= 0.5f;
        }
    }

    public void Init()
    {
        leftLoopState = IKLoopState.Apex;
        rightLoopState = IKLoopState.Contact;
        leftTimer = 0f;
        rightTimer = 0f;
        OnDisable();
        if (!IsInstanceValid(forwardNode))
        {
            forwardNode = humanoid;
        }
        if (IsInstanceValid(humanoid) /*&& humanoid.avatar && humanoid.avatar.isHuman*/)
        {
            humanBindPose = humanoid.Transform;
            /*
            head = humanoid.GetBoneTransform(HumanBodyBones.Head);
            hips = humanoid.GetBoneTransform(HumanBodyBones.Hips);
            leftHand = humanoid.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHand = humanoid.GetBoneTransform(HumanBodyBones.RightHand);
            leftFoot = humanoid.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = humanoid.GetBoneTransform(HumanBodyBones.RightFoot);
            direction = humanoid.GetBoneTransform(HumanBodyBones.Head).position - humanoid.GetBoneTransform(HumanBodyBones.Hips).position;
            */
            direction = head.GlobalPosition - hips.GlobalPosition;
            float scl = direction.Length() / GetScale();
            footDistance = leftFoot.GlobalPosition.DistanceTo(rightFoot.GlobalPosition);
            hipsDistance = hips.GlobalPosition.DistanceTo(head.GlobalPosition);
            floorDistance = hips.GlobalPosition.DistanceTo(humanoid.GlobalPosition);
            float scale = 1f / GetScale();
            float handPoleDist = -1f;

            Vector3 right = humanoid.GlobalBasis.X.Normalized();
            Vector3 forward = -humanoid.GlobalBasis.Z.Normalized();
            Vector3 up = humanoid.GlobalBasis.Y.Normalized();

            // hips and head
            {
                headTarget = new Node3D() { Name = "Head_Target" };
                humanoid.AddChild(headTarget);
                headTarget.GlobalTransform = head.GlobalTransform;
                hipsPos = hips.GlobalPosition;
            }

            // left hand
            {
                leftHandData.target = new Node3D() { Name = "LeftHand_Target" };
                humanoid.AddChild(leftHandData.target);
                leftHandData.target.GlobalTransform = leftHand.GlobalTransform;
                leftHandData.pole = new Node3D() { Name = "LeftHand_Pole" };
                head.AddChild(leftHandData.pole);
                leftHandData.pole.GlobalPosition = head.GlobalPosition - right * scl - forward * scl + up * handPoleDist * scl;

                FastIKFabric ik = new FastIKFabric();
                // leftHand.AddChild(ik);
                ik.Enabled = false;
                ik.Target = leftHandData.target;
                ik.Pole = leftHandData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                // ik.Init();
                leftHand.AddChild(ik);
                leftHandData.ik = ik;
            }
            // right hand
            {
                rightHandData.target = new Node3D() { Name = "RightHand_Target" };
                humanoid.AddChild(rightHandData.target);
                rightHandData.target.GlobalTransform = rightHand.GlobalTransform;
                rightHandData.pole = new Node3D() { Name = "RightHand_Pole" };
                head.AddChild(rightHandData.pole);
                rightHandData.pole.GlobalPosition = head.GlobalPosition + right * scl - forward * scl + up * handPoleDist * scl;

                FastIKFabric ik = new FastIKFabric();
                // rightHand.AddChild(ik);
                ik.Enabled = false;
                ik.Target = rightHandData.target;
                ik.Pole = rightHandData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                // ik.Init();
                rightHand.AddChild(ik);
                rightHandData.ik = ik;
            }

            // left foot
            {
                leftFootData.target = new Node3D() { Name = "LeftFoot_Target" };
                humanoid.AddChild(leftFootData.target);
                leftFootData.target.GlobalTransform = leftFoot.GlobalTransform;
                leftFootData.pole = new Node3D() { Name = "LeftFoot_Pole" };
                hips.AddChild(leftFootData.pole);
                leftFootData.pole.GlobalPosition = leftFoot.GlobalPosition - right * 0f * scl + forward * 0.5f * scl + up * 0.5f * scl;

                FastIKFabric ik = new FastIKFabric();
                // leftFoot.AddChild(ik);
                ik.Enabled = false;
                ik.Target = leftFootData.target;
                ik.Pole = leftFootData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                // ik.Init();
                leftFoot.AddChild(ik);
                leftFootData.ik = ik;
                leftFootPos = leftFoot.GlobalPosition;
            }
            // right foot
            {
                rightFootData.target = new Node3D() { Name = "RightFoot_Target" };
                humanoid.AddChild(rightFootData.target);
                rightFootData.target.GlobalTransform = rightFoot.GlobalTransform;
                rightFootData.pole = new Node3D() { Name = "RightFoot_Pole" };
                hips.AddChild(rightFootData.pole);
                rightFootData.pole.GlobalPosition = rightFoot.GlobalPosition + right * 0f * scl + forward * 0.5f * scl + up * 0.5f * scl;

                FastIKFabric ik = new FastIKFabric();
                // rightFoot.AddChild(ik);
                ik.Enabled = false;
                ik.Target = rightFootData.target;
                ik.Pole = rightFootData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                // ik.Init();
                rightFoot.AddChild(ik);
                rightFootData.ik = ik;
                rightFootPos = rightFoot.GlobalPosition;
            }

            // head.OverridePose = true;
            // hips.OverridePose = true;
            // leftHand.OverridePose = true;
            // rightHand.OverridePose = true;
            // leftFoot.OverridePose = true;
            // rightFoot.OverridePose = true;
        }
    }

    // https://github.com/MMMaellon/renik/blob/master/RenIK%20Foot%20Placement.png
    private Vector2 EvaluateLoop(ref IKLoopState state, ref float offset, float speed, float dt, float phase, bool canAdv)
    {
        float realOffset = Mathf.Clamp(offset + phase, 0f, 1f);
        Vector2 apexPt = new Vector2(speed, speed * 0.7f);
        Vector2 buildupPt = new Vector2(speed * 0.8f, speed * 0.5f);
        Vector2 contactPt = new Vector2(speed * 0.3f, 0f);
        Vector2 followPt = new Vector2(-speed * 0.1f, 0f);
        Vector2 thruPt = new Vector2(-speed, speed * 0.4f);
        Vector2 point = Vector2.Zero;
        switch (state)
        {
            case IKLoopState.Apex:
                point = apexPt.Lerp(buildupPt, realOffset);
                offset += dt * 1.5f;
                break;
            case IKLoopState.Buildup:
                point = buildupPt.Lerp(contactPt, realOffset);
                offset += dt * 2f;
                break;
            case IKLoopState.Contact:
                point = contactPt.Lerp(thruPt, realOffset);
                // point = Vector2.Lerp(contactPt, followPt, realOffset);
                offset += dt * 2f;
                break;
            case IKLoopState.FollowThru:
                point = followPt.Lerp(thruPt, realOffset);
                offset += dt * 0.5f;
                break;
            case IKLoopState.ThruApex:
                point = thruPt.Lerp(apexPt, realOffset);
                offset += dt;
                break;
        }
        if (offset + phase >= 1f && canAdv)
        {
            offset = -phase;
            switch (state)
            {
                case IKLoopState.Apex:
                    state = IKLoopState.Buildup;
                    break;
                case IKLoopState.Buildup:
                    state = IKLoopState.Contact;
                    break;
                case IKLoopState.Contact:
                    state = IKLoopState.ThruApex;
                    break;
                case IKLoopState.FollowThru:
                    state = IKLoopState.ThruApex;
                    break;
                case IKLoopState.ThruApex:
                    state = IKLoopState.Apex;
                    break;
            }
        }
        return point;
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
        // head.OverridePose = false;
        // hips.OverridePose = false;
        // leftHand.OverridePose = false;
        // rightHand.OverridePose = false;
        // leftFoot.OverridePose = false;
        // rightFoot.OverridePose = false;
    }

#if false
    private void OnDrawGizmosSelected()
    {
        void DrawIKData(IKData data)
        {
            Gizmos.color = Color.red;
            if (data.target)
                Gizmos.DrawSphere(data.target.position, 0.1f);
            Gizmos.color = Color.blue;
            if (data.pole)
                Gizmos.DrawSphere(data.pole.position, 0.1f);
        }
        DrawIKData(leftHandData);
        DrawIKData(rightHandData);
        DrawIKData(leftFootData);
        DrawIKData(rightFootData);
    }
#endif

}
#endif