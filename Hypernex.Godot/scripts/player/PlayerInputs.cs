using System.Collections.Generic;
using Godot;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.Game;

namespace Hypernex.Player
{
    // This whole class will likely become abstract later on
    public partial class PlayerInputs : Node
    {
        [Export]
        public PlayerRoot root;
        [Export]
        public RayCast3D uiCast;
        [Export]
        public RayCast3D grabCast;

        public GrabbableDescriptor grabbedObject = null;
        public float grabDistance = 1f;

        public Vector2 move;
        public bool textChatOpen;
        public Vector2 lastMousePosition;
        public Vector2 lastMouseDelta;
        public bool shouldJump = false;
        public bool isNoclip = false;

        private bool lastGrabTriggerState = false;
        private bool lastPrimaryTriggerState = false;

        public override void _Ready()
        {
            lastMousePosition = GetViewport().GetMousePosition();
        }

        public override void _ExitTree()
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.MouseMode == Input.MouseModeEnum.Visible)
                return;
            if (Init.IsVRLoaded)
                return;
            if (@event is InputEventMouseMotion mouseMotion)
            {
                lastMouseDelta = mouseMotion.Relative * -0.001f;
            }
        }

        public override void _Process(double delta)
        {
            if (Input.IsActionJustPressed("ui_cancel") && GetWindow().HasFocus())
            {
                textChatOpen = false;
                Input.MouseMode = (Input.MouseMode == Input.MouseModeEnum.Visible) ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
                Init.Instance.ui.Visible = Input.MouseMode == Input.MouseModeEnum.Visible;
            }
            if (Input.MouseMode == Input.MouseModeEnum.Visible)
                return;
            var position = GetViewport().GetMousePosition();
            lastMousePosition = position;
        }

        public override void _PhysicsProcess(double delta)
        {
            ReadInput();
            HandleMouseRayCast();
            HandleMouseGrab(delta);
        }

        public void HandleMouseGrab(double delta)
        {
            if (Init.IsVRLoaded)
            {
                grabCast.Visible = false;
                return;
            }
            bool triggerState = Input.IsMouseButtonPressed(MouseButton.Left) && !Init.Instance.ui.IsVisibleInTree();
            if (IsInstanceValid(grabbedObject))
            {
                var vel = (root.view.GlobalPosition + root.view.GlobalBasis.Z * -grabDistance - grabbedObject.parent.GlobalPosition) * grabbedObject.VelocityAmount;
                if (triggerState)
                {
                    if (grabbedObject.parent is RigidBody3D rb2)
                    {
                        rb2.LinearVelocity = vel;
                    }
                    else if (grabbedObject.parent is PhysicsBody3D pb)
                    {
                        vel *= (float)delta;
                        pb.MoveAndCollide(vel);
                    }
                }
                else
                {
                    if (grabbedObject.parent is RigidBody3D rb)
                    {
                        rb.LinearVelocity = vel;
                        rb.GravityScale = 1f;
                    }
                    grabbedObject = null;
                }
                return;
            }
            if (grabCast.IsColliding())
            {
                bool shouldGrab = false;
                if (triggerState != lastGrabTriggerState)
                {
                    shouldGrab = true;
                }
                lastGrabTriggerState = triggerState;
                GodotObject collider = grabCast.GetCollider();
                if (collider.HasMeta(GrabbableDescriptor.TypeName))
                {
                    try
                    {
                        grabCast.Visible = true;
                        GrabbableDescriptor grabbable = collider.GetMeta(GrabbableDescriptor.TypeName).As<GrabbableDescriptor>();
                        if (shouldGrab && grabbable.parent is PhysicsBody3D b)
                        {
                            grabbedObject = grabbable;
                            grabDistance = root.view.GlobalPosition.DistanceTo(grabbedObject.parent.GlobalPosition);
                            if (b is RigidBody3D rb)
                                rb.GravityScale = 0f;
                        }
                    }
                    catch
                    { }
                }
                else
                {
                    grabCast.Visible = false;
                }
            }
            else
            {
                grabCast.Visible = false;
                lastGrabTriggerState = false;
            }
        }

        public void HandleMouseRayCast()
        {
            if (Init.IsVRLoaded)
            {
                uiCast.Visible = false;
                return;
            }
            if (uiCast.IsColliding())
            {
                bool triggerState = Input.IsMouseButtonPressed(MouseButton.Left) && !Init.Instance.ui.IsVisibleInTree();
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
                lastPrimaryTriggerState = triggerState;
                GodotObject collider = uiCast.GetCollider();
                if (collider.HasMeta(UICanvas.TypeName))
                {
                    // prevent people from fooling with the cck
                    try
                    {
                        uiCast.Visible = true;
                        UICanvas canvas = collider.GetMeta(UICanvas.TypeName).As<UICanvas>();
                        canvas.HandleInput(root.view, ev, uiCast.GetCollisionPoint(), uiCast.GetCollisionNormal(), uiCast.GetColliderShape());
                    }
                    catch
                    { }
                }
                else
                {
                    uiCast.Visible = false;
                }
            }
            else
            {
                uiCast.Visible = false;
                lastPrimaryTriggerState = false;
            }
        }

        public virtual void ReadInput()
        {
            if (Init.IsVRLoaded)
                return;
            if (Input.IsActionJustReleased("chat_text") && GetWindow().HasFocus())
            {
                textChatOpen = true;
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
            if (textChatOpen)
            {
                move = Vector2.Zero;
            }
            else
            {
                move = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
                if (Input.IsActionJustPressed("move_jump"))
                    shouldJump = true;
                if (Input.IsActionJustPressed("move_noclip"))
                    isNoclip = !isNoclip;
            }
        }
    }
}
