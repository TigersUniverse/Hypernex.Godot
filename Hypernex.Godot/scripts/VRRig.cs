using System;
using Godot;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.Game;
using Hypernex.Player;
using Hypernex.Tools;

public partial class VRRig : Node3D
{
    [Export]
    public XROrigin3D origin;
    [Export]
    public XRCamera3D head;
    [Export]
    public XRController3D leftHand;
    [Export]
    public XRController3D rightHand;
    [Export]
    public XRController3D leftHandSkel;
    [Export]
    public XRController3D rightHandSkel;
    [Export]
    public RayCast3D[] raycasts = Array.Empty<RayCast3D>();

    private bool lastLeftTriggerState = false;
    private bool lastRightTriggerState = false;
    private bool lastPrimaryTriggerState = false;
    private string primaryTracker;

    [Export]
    public Node3D openMenuWarning;
    [Export]
    public float triggerClickThreshold = 0.75f;

    public override void _Ready()
    {
        XRServer.WorldScale = 1d;
    }

    public override void _Process(double delta)
    {
        openMenuWarning.Visible = Init.Instance.ui.Visible && GameInstance.FocusedInstance == null;

        foreach (var cast in raycasts)
        {
            XRController3D ctrl = cast.GetParentOrNull<XRController3D>();
            if (!IsInstanceValid(ctrl))
                continue;
            if (cast.IsColliding())
            {
                if (ctrl.Tracker != primaryTracker)
                {
                    cast.Visible = false;
                    continue;
                }
                bool triggerState = ctrl.GetFloat("trigger") > triggerClickThreshold;
                InputEventMouse ev;
                if (triggerState != lastPrimaryTriggerState)
                {
                    ev = new InputEventMouseButton()
                    {
                        ButtonIndex = MouseButton.Left,
                        Pressed = triggerState,
                    };
                }
                else
                {
                    ev = new InputEventMouseMotion()
                    {
                        ButtonMask = triggerState ? MouseButtonMask.Left : 0,
                    };
                }
                lastPrimaryTriggerState = ctrl.GetFloat("trigger") > triggerClickThreshold;
                GodotObject collider = cast.GetCollider();
                if (collider.HasMeta(UICanvas.TypeName))
                {
                    // prevent people from fooling with the cck
                    try
                    {
                        cast.Visible = true;
                        UICanvas canvas = collider.GetMeta(UICanvas.TypeName).As<UICanvas>();
                        canvas.HandleInput(head, ev, cast.GetCollisionPoint(), cast.GetCollisionNormal(), cast.GetColliderShape());
                    }
                    catch
                    { }
                }
            }
            else
            {
                cast.Visible = false;
                if (ctrl.Tracker == primaryTracker)
                    lastPrimaryTriggerState = false;
            }
        }

        bool leftTriggerState = leftHand.GetFloat("trigger") > triggerClickThreshold;
        bool rightTriggerState = rightHand.GetFloat("trigger") > triggerClickThreshold;

        if (lastLeftTriggerState != leftTriggerState)
            primaryTracker = leftHand.Tracker;
        else if (lastRightTriggerState != rightTriggerState)
            primaryTracker = rightHand.Tracker;

        lastLeftTriggerState = leftTriggerState;
        lastRightTriggerState = rightTriggerState;

        if (IsInstanceValid(PlayerRoot.Local))
        {
            origin.GlobalPosition = PlayerRoot.Local.Controller.GlobalPosition;
            origin.GlobalRotation = PlayerRoot.Local.Controller.GlobalRotation;
            PlayerRoot.Local.view.GlobalTransform = head.GlobalTransform;
            PlayerInputs inputs = PlayerRoot.Local.GetPart<PlayerInputs>();
            inputs.move = leftHand.GetVector2("primary") * new Vector2(1f, -1f);
            inputs.lastMouseDelta.X = -rightHand.GetVector2("primary").X * (float)delta * Mathf.Pi;
            if (IsInstanceValid(PlayerRoot.Local.GetPart<PlayerChat>().voice))
                PlayerRoot.Local.GetPart<PlayerChat>().voice.Recording = rightHand.GetFloat("ax_button") > 0.5f;
            if (IsInstanceValid(PlayerRoot.Local.Avatar))
            {
                XRServer.WorldScale = PlayerRoot.Local.Avatar.ikSystem.floorDistance;
                Transform3D floor = head.GlobalTransform;
                floor.Origin += Vector3.Down * head.Position.Y;
                // TODO: eye offsets
                Vector3 backFloor = head.GlobalBasis.Z;
                backFloor.Y = 0f;
                PlayerRoot.Local.Avatar.ProcessIk(true, true, head.GlobalTransform.Translated(backFloor * 0.2f), floor, leftHandSkel.GlobalTransform, rightHandSkel.GlobalTransform);
                // PlayerRoot.Local.Avatar.ikSystem.head.Scale = Vector3.One * 0.01f;
            }
        }
    }
}