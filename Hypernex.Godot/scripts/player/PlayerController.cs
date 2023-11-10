using System.Collections.Generic;
using Godot;
using Hypernex.Game;

namespace Hypernex.Player
{
    public partial class PlayerController : CharacterBody3D
    {
        [Export]
        public PlayerRoot root;
        public PlayerInputs inputs;
        [Export]
        public float accel = 1f;
        [Export]
        public float speed = 1f;
        public float gravity;

        public override void _Ready()
        {
            inputs = root.GetPart<PlayerInputs>();
            gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        }

        public override void _Process(double delta)
        {
            Rotation = new Vector3(0f, inputs.totalMousePosition.X, 0f);
            root.view.Rotation = new Vector3(inputs.totalMousePosition.Y, 0f, 0f);
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector3 vel = Velocity;

            if (!IsOnFloor())
                vel.Y -= gravity * (float)delta;
            else
                vel.Y = 0f;

            Vector3 dir = root.Transform.Basis * Transform.Basis * (new Vector3(inputs.move.X, 0f, inputs.move.Y).Normalized() * speed);
            dir.Y = vel.Y;
            vel = vel.MoveToward(dir, (float)delta * accel);

            Velocity = vel;
            MoveAndSlide();
        }
    }
}
