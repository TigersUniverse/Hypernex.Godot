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
            hips.Position = hips.GetParentNode3D().ToLocal(headTarget.GlobalPosition) - hips.GetParentNode3D().ToLocal(direction);
            // hips.Position = hips.GetParentNode3D().InverseTransformPoint(headTarget.GlobalPosition) - hips.GetParentNode3D().InverseTransformVector(direction);
            head.GlobalPosition = headTarget.GlobalPosition;
            head.GlobalBasis = headTarget.GlobalBasis;
        }

        Vector3 vel = hips.GlobalPosition - hipsPos;
#if false
        bool found = false;
        if (leftFootData.target && leftFootData.ik)
        {
            var scl = humanoid.transform.right + humanoid.transform.forward;
            var footPos = Vector3.Scale(leftFootData.target.position, scl);
            var otherFootPos = Vector3.Scale(rightFootData.target.position, scl);
            var hipsPos = Vector3.Scale(hips.position, scl);
            var dir = hipsPos - footPos;
            var otherDir = hipsPos - otherFootPos;
            float amount = 1f;
            // if (Vector3.Dot(dir, humanoid.transform.right) < 0f)
            //     amount = 2f;
            if (leftFootData.ik.ReachAmountSqr >= Mathf.Pow(reachDistance * leftFootData.ik.CompleteLength, 2f) /*&& timer <= 0f*/ && flipFlop)
            // if (leftFootData.ik.IsOutOfReach && /*timer <= 0f &&*/ flipFlop)
            {
                found = true;
                leftTimer = 0.1f;
                timer = 0.1f;
                leftFootPos = leftFootData.target.position + dir.normalized * (stepDistance * amount * leftFootData.ik.CompleteLength * velCurve.Evaluate(velTimer));
                flipFlop = !flipFlop;
            }
            leftFootData.target.position = Vector3.Lerp(leftFootData.target.position, leftFootPos, Time.deltaTime * footMoveSpeed);
            if ((leftFootData.target.position - leftFootPos).sqrMagnitude >= 0.1f * 0.1f)
            {
                leftTimer = 0.1f;
                timer = 0.1f;
            }
        }
        if (rightFootData.target && rightFootData.ik)
        {
            var scl = humanoid.transform.right + humanoid.transform.forward;
            var footPos = Vector3.Scale(rightFootData.target.position, scl);
            var otherFootPos = Vector3.Scale(leftFootData.target.position, scl);
            var hipsPos = Vector3.Scale(hips.position, scl);
            var dir = hipsPos - footPos;
            float amount = 1f;
            // if (Vector3.Dot(dir, humanoid.transform.right) > 0f)
            //     amount = 2f;
            if (rightFootData.ik.ReachAmountSqr >= Mathf.Pow(reachDistance * rightFootData.ik.CompleteLength, 2f) /*&& timer <= 0f*/ && !flipFlop && !found)
            // if (rightFootData.ik.IsOutOfReach && /*timer <= 0f &&*/ !flipFlop && !found)
            {
                rightTimer = 0.1f;
                timer = 0.1f;
                rightFootPos = rightFootData.target.position + dir.normalized * (stepDistance * amount * rightFootData.ik.CompleteLength * velCurve.Evaluate(velTimer));
                flipFlop = !flipFlop;
            }
            rightFootData.target.position = Vector3.Lerp(rightFootData.target.position, rightFootPos, Time.deltaTime * footMoveSpeed);
            if ((rightFootData.target.position - rightFootPos).sqrMagnitude >= 0.1f * 0.1f)
            {
                rightTimer = 0.1f;
                timer = 0.1f;
            }
        }
        if (timer > 0f)
            timer -= Time.deltaTime;
        if (leftTimer > 0f)
            leftTimer -= Time.deltaTime;
        if (rightTimer > 0f)
            rightTimer -= Time.deltaTime;
        // if (velTimer > 0f)
            velTimer += Time.deltaTime;
        if (vel.magnitude > Time.deltaTime * 0.1f)
            velTimer = 0f;
#endif

#if false
        if (moveFeet)
        {
            timer += vel.magnitude;
            float speed = vel.magnitude / Time.deltaTime;

            // left foot
            {
                float y = MapValue(Mathf.Sin(timer + Mathf.Deg2Rad * 180f), -1f, 1f, minStepHeight * speed, maxStepHeight * speed);
                float z = MapValue(-Mathf.Cos(timer + Mathf.Deg2Rad * 180f), -1f, 1f, minStepLength * speed, maxStepLength * speed);

                leftFootData.target.localPosition = new Vector3(footDistance * -0.5f, y, z);
            }
            // right foot
            {
                float y = MapValue(Mathf.Sin(timer), -1f, 1f, minStepHeight * speed, maxStepHeight * speed);
                float z = MapValue(-Mathf.Cos(timer), -1f, 1f, minStepLength * speed, maxStepLength * speed);

                rightFootData.target.localPosition = new Vector3(footDistance * 0.5f, y, z);
            }
        }
#endif

        if (moveFeet)
        {
            float speed = lastVel.Length();
            // speed = 1f;
            float scale = 1f / humanoid.GlobalBasis.Scale.Z;
            float unitScaleHeight = scale * hipsDistance;
            float unitScaleLength = scale;
            float scaleLength = lastVel.Length();
            // scaleLength = 1f;
            {
                Vector2 pos = EvaluateLoop(ref leftLoopState, ref leftTimer, loopSize, speed, 0f);
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
                else
                    leftTimer = 0f;
                end += new Vector3(x, 0f, 0f);
                leftFootData.target.Position = end;
            }
            {
                rightLoopState = (IKLoopState)(((int)leftLoopState + 2) % (int)IKLoopState.Max);
                rightTimer = leftTimer;

                Vector2 pos = EvaluateLoop(ref rightLoopState, ref rightTimer, loopSize, speed, 0f);
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
                else
                    rightTimer = 0f;
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
                lastVel = newvel;
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
            float scl = /*direction.Length()*/ 1f / humanoid.GlobalBasis.Scale.Z;
            footDistance = leftFoot.GlobalPosition.DistanceTo(rightFoot.GlobalPosition);
            hipsDistance = hips.GlobalPosition.DistanceTo(head.GlobalPosition);
            float scale = 1f / humanoid.GlobalBasis.Scale.Z;

            Vector3 right = -humanoid.GlobalBasis.X.Normalized();
            Vector3 forward = humanoid.GlobalBasis.Z.Normalized();
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
                leftHandData.pole.GlobalPosition = head.GlobalPosition - right * scl - forward * scl + up * 0.5f * scl;

                FastIKFabric ik = new FastIKFabric();
                leftHand.AddChild(ik);
                ik.Target = leftHandData.target;
                ik.Pole = leftHandData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                ik.Init();
                leftHandData.ik = ik;
            }
            // right hand
            {
                rightHandData.target = new Node3D() { Name = "RightHand_Target" };
                humanoid.AddChild(rightHandData.target);
                rightHandData.target.GlobalTransform = rightHand.GlobalTransform;
                rightHandData.pole = new Node3D() { Name = "RightHand_Pole" };
                head.AddChild(rightHandData.pole);
                rightHandData.pole.GlobalPosition = head.GlobalPosition + right * scl - forward * scl + up * 0.5f * scl;

                FastIKFabric ik = new FastIKFabric();
                rightHand.AddChild(ik);
                ik.Target = rightHandData.target;
                ik.Pole = rightHandData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                ik.Init();
                rightHandData.ik = ik;
            }

            // left foot
            {
                leftFootData.target = new Node3D() { Name = "LeftFoot_Target" };
                humanoid.AddChild(leftFootData.target);
                leftFootData.target.GlobalTransform = leftFoot.GlobalTransform;
                leftFootData.pole = new Node3D() { Name = "LeftFoot_Pole" };
                hips.AddChild(leftFootData.pole);
                leftFootData.pole.GlobalPosition = leftUpperLeg.GlobalPosition - right * 0.25f * scl + forward * 1.5f * scl + up * 0.9f * scl;

                FastIKFabric ik = new FastIKFabric();
                leftFoot.AddChild(ik);
                ik.Target = leftFootData.target;
                ik.Pole = leftFootData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                ik.Init();
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
                rightFootData.pole.GlobalPosition = rightUpperLeg.GlobalPosition + right * 0.25f * scl + forward * 1.5f * scl + up * 0.9f * scl;

                FastIKFabric ik = new FastIKFabric();
                rightFoot.AddChild(ik);
                ik.Target = rightFootData.target;
                ik.Pole = rightFootData.pole;
                ik.ChainLength = 2;
                ik.SnapBackStrength = SnapBackStrength;
                ik.Init();
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
    private Vector2 EvaluateLoop(ref IKLoopState state, ref float offset, float speed, float dt, float phase)
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
        if (offset + phase >= 1f)
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
        head.OverridePose = false;
        hips.OverridePose = false;
        leftHand.OverridePose = false;
        rightHand.OverridePose = false;
        leftFoot.OverridePose = false;
        rightFoot.OverridePose = false;
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