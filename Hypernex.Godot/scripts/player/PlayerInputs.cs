using System.Collections.Generic;
using Godot;
using Hypernex.Game;

namespace Hypernex.Player
{
    public partial class PlayerInputs : Node
    {
        [Export]
        public PlayerRoot root;

        public Vector2 move;
        public Vector2 lastMousePosition;
        public Vector2 lastMouseDelta;
        public Vector2 totalMousePosition;

        public override void _Ready()
        {
            lastMousePosition = GetViewport().GetMousePosition();
        }

        public override void _Input(InputEvent @event)
        {
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
            move = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        }
    }
}
