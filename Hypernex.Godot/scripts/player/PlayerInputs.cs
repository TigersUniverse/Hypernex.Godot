using System.Collections.Generic;
using Godot;
using Hypernex.Game;

namespace Hypernex.Player
{
    // This whole class will likely become abstract later on
    public partial class PlayerInputs : Node
    {
        [Export]
        public PlayerRoot root;

        public Vector2 move;
        public bool textChatOpen;
        public Vector2 lastMousePosition;
        public Vector2 lastMouseDelta;
        public Vector2 totalMousePosition;

        public override void _Ready()
        {
            lastMousePosition = GetViewport().GetMousePosition();
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        public override void _ExitTree()
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.MouseMode == Input.MouseModeEnum.Visible)
                return;
            if (@event is InputEventMouseMotion mouseMotion)
            {
                lastMouseDelta -= mouseMotion.Relative;
            }
        }

        public override void _Process(double delta)
        {
            if (Input.IsActionJustPressed("ui_cancel"))
            {
                Input.MouseMode = (Input.MouseMode == Input.MouseModeEnum.Visible) ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
                Init.Instance.ui.Visible = Input.MouseMode == Input.MouseModeEnum.Visible;
            }
            if (Input.MouseMode == Input.MouseModeEnum.Visible)
                return;
            var position = GetViewport().GetMousePosition();
            // lastMouseDelta = lastMousePosition - position;
            lastMousePosition = position;
            totalMousePosition += lastMouseDelta * 0.005f;
            lastMouseDelta = Vector2.Zero;
            ReadInput();
        }

        public override void _PhysicsProcess(double delta)
        {
            ReadInput();
        }

        public virtual void ReadInput()
        {
            if (Input.IsActionJustReleased("chat_text"))
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
