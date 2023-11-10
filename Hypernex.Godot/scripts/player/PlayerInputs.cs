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

        public override void _Process(double delta)
        {
            var position = GetViewport().GetMousePosition();
            lastMouseDelta = lastMousePosition - position;
            lastMousePosition = position;
            totalMousePosition += lastMouseDelta * 0.005f;
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
