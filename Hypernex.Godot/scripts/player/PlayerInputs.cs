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
        public RayCast3D cast;

        public Vector2 move;
        public bool textChatOpen;
        public Vector2 lastMousePosition;
        public Vector2 lastMouseDelta;

        private bool lastPrimaryTriggerState = false;

        public override void _Ready()
        {
            lastMousePosition = GetViewport().GetMousePosition();
            // Input.MouseMode = Input.MouseModeEnum.Captured;
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
                lastMouseDelta = mouseMotion.Relative * -0.005f;
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
            ReadInput();
            HandleMouseRayCast();
        }

        public override void _PhysicsProcess(double delta)
        {
            ReadInput();
            HandleMouseRayCast();
        }

        public void HandleMouseRayCast()
        {
            if (Init.IsVRLoaded)
            {
                cast.Visible = false;
                return;
            }
            if (cast.IsColliding())
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
                GodotObject collider = cast.GetCollider();
                if (collider.HasMeta(UICanvas.TypeName))
                {
                    // prevent people from fooling with the cck
                    try
                    {
                        cast.Visible = true;
                        UICanvas canvas = collider.GetMeta(UICanvas.TypeName).As<UICanvas>();
                        canvas.HandleInput(root.view, ev, cast.GetCollisionPoint(), cast.GetCollisionNormal(), cast.GetColliderShape());
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
            }
        }
    }
}
