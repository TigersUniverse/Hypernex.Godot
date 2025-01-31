using System;
using System.Collections.Generic;
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
    public XRController3D hips;
    [Export]
    public XRController3D leftFoot;
    [Export]
    public XRController3D rightFoot;
    [Export]
    public RayCast3D[] raycasts = Array.Empty<RayCast3D>();
    [Export]
    public UICanvas canvas;
    [Export]
    public Godot.Range voiceMeter;

    [Export]
    public SubViewport vp;
    [Export]
    public Camera3D cam;

    private bool lastLeftTriggerState = false;
    private bool lastRightTriggerState = false;
    private bool lastPrimaryTriggerState = false;
    private bool lastVoiceState = false;
    private bool lastJumpState = false;
    private string primaryTracker;
    private bool lastMenuToggleState = false;

    public bool IsFBT => hips.GetIsActive() && leftFoot.GetIsActive() && rightFoot.GetIsActive();

    [Export]
    public float triggerClickThreshold = 0.75f;

    public override void _Ready()
    {
        XRServer.WorldScale = 1d;
    }

    public override void _EnterTree()
    {
        if (IsInstanceValid(Init.Instance))
        {
            Init.Instance.ui.Reparent(canvas.VP, false);
        }
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(Init.Instance))
        {
            Init.Instance.ui.Reparent(Init.Instance, false);
        }
    }

    public override void _Process(double delta)
    {
        cam.GlobalTransform = head.GlobalTransform;
        bool menuToggleState = rightHand.GetFloat("by_button") > 0.5f;
        if (menuToggleState && lastMenuToggleState != menuToggleState)
        {
            canvas.Visible = !canvas.Visible;
        }
        lastMenuToggleState = menuToggleState;

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
                Vector2 scroll = ctrl.GetVector2("primary");
                List<InputEventMouse> ev = new List<InputEventMouse>();
                if (triggerState != lastPrimaryTriggerState)
                {
                    ev.Add(new InputEventMouseButton()
                    {
                        ButtonIndex = MouseButton.Left,
                        Pressed = triggerState,
                    });
                }
                else
                {
                    ev.Add(new InputEventMouseMotion()
                    {
                        ButtonMask = triggerState ? MouseButtonMask.Left : 0,
                    });
                    if (scroll.Y > 0.15f)
                    {
                        ev.Add(new InputEventMouseButton()
                        {
                            ButtonIndex = MouseButton.WheelUp,
                            Factor = scroll.Y,
                            Pressed = true,
                        });
                    }
                    else if (scroll.Y < -0.15f)
                    {
                        ev.Add(new InputEventMouseButton()
                        {
                            ButtonIndex = MouseButton.WheelDown,
                            Factor = -scroll.Y,
                            Pressed = true,
                        });
                    }
                    else
                    {
                        ev.Add(new InputEventMouseButton()
                        {
                            ButtonIndex = MouseButton.WheelUp,
                            Pressed = false,
                        });
                        ev.Add(new InputEventMouseButton()
                        {
                            ButtonIndex = MouseButton.WheelDown,
                            Pressed = false,
                        });
                    }
                }
                lastPrimaryTriggerState = triggerState;
                GodotObject collider = cast.GetCollider();
                if (collider.HasMeta(UICanvas.TypeName))
                {
                    // prevent people from fooling with the cck
                    try
                    {
                        UICanvas canvas = collider.GetMeta(UICanvas.TypeName).As<UICanvas>();
                        if (!canvas.IsVisibleInTree())
                        {
                            cast.Visible = false;
                            continue;
                        }
                        cast.Visible = true;
                        foreach (var e in ev)
                        {
                            canvas.HandleInput(head, e, cast.GetCollisionPoint(), cast.GetCollisionNormal(), cast.GetColliderShape());
                        }
                    }
                    catch
                    { }
                }
                else
                {
                    cast.Visible = false;
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

        bool voiceState = rightHand.GetFloat("ax_button") > 0.5f;
        bool jumpState = rightHand.GetFloat("primary_click") > 0.5f;

        if (IsInstanceValid(PlayerRoot.Local))
        {
            origin.GlobalPosition = PlayerRoot.Local.Controller.GlobalPosition;
            origin.GlobalRotation = PlayerRoot.Local.Controller.GlobalRotation;
            PlayerRoot.Local.view.GlobalTransform = head.GlobalTransform;
            PlayerInputs inputs = PlayerRoot.Local.GetPart<PlayerInputs>();
            inputs.move = leftHand.GetVector2("primary") * new Vector2(1f, -1f);
            if (jumpState)
                inputs.shouldJump = true;
            inputs.lastMouseDelta.X = -rightHand.GetVector2("primary").X * (float)delta * Mathf.Pi;
            if (IsInstanceValid(PlayerRoot.Local.GetPart<PlayerChat>().voice))
            {
                var voice = PlayerRoot.Local.GetPart<PlayerChat>().voice;
                if (voiceState != lastVoiceState && voiceState)
                    voice.Recording = !voice.Recording;
                if (IsInstanceValid(voiceMeter))
                {
                    voiceMeter.Visible = voice.IsSpeaking;
                    voiceMeter.Value = voice.Loudness * 80f;
                }
            }
            if (IsInstanceValid(PlayerRoot.Local.Avatar))
            {
                // The 1.75 is the player's real world height, placeholder for now.
                XRServer.WorldScale = (PlayerRoot.Local.Avatar.ikSystem.floorDistance + PlayerRoot.Local.Avatar.ikSystem.hipsDistance * 0.75f) / 1.75f;
                Transform3D floor = head.GlobalTransform;
                floor.Origin += Vector3.Down * head.Position.Y;
                // TODO: eye offsets
                Vector3 backFloor = head.GlobalBasis.Z;
                backFloor.Y = 0f;
                PlayerRoot.Local.Avatar.ikSystem.moveFeet = !IsFBT;
                if (IsFBT)
                    PlayerRoot.Local.Avatar.ProcessIkFBT(true, head.GlobalTransform.Translated(backFloor * 0.2f), floor, leftHandSkel.GlobalTransform, rightHandSkel.GlobalTransform, hips.GlobalTransform, leftFoot.GlobalTransform, rightFoot.GlobalTransform);
                else
                    PlayerRoot.Local.Avatar.ProcessIk(true, true, head.GlobalTransform.Translated(backFloor * 0.2f), floor, leftHandSkel.GlobalTransform, rightHandSkel.GlobalTransform);
                // PlayerRoot.Local.Avatar.ikSystem.head.Scale = Vector3.One * 0.01f;
            }
        }
        else
        {
            if (IsInstanceValid(voiceMeter))
                voiceMeter.Visible = false;
        }

        lastVoiceState = voiceState;
        lastJumpState = jumpState;
    }
}