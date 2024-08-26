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
        [Export]
        public float jumpHeight = 2f;
        public float gravity;
        [Export]
        public Camera3D cam;

        public override void _Ready()
        {
            if (IsInstanceValid(cam))
                cam.MakeCurrent();
            inputs = root.GetPart<PlayerInputs>();
            gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        }

        public override void _Process(double delta)
        {
            if (inputs == null)
                return;
            Rotation += new Vector3(0f, inputs.lastMouseDelta.X, 0f);
            root.view.Rotation = new Vector3(Mathf.Clamp(root.view.Rotation.X + inputs.lastMouseDelta.Y, Mathf.DegToRad(-89f), Mathf.DegToRad(89f)), 0f, 0f);
            inputs.lastMouseDelta = Vector2.Zero;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (inputs == null)
                return;
            Vector3 vel = Velocity;

            if (!IsOnFloor())
                vel.Y -= gravity * (float)delta;
            else if (inputs.shouldJump)
                vel.Y = jumpHeight;
            else
                vel.Y = 0f;

            inputs.shouldJump = false;
            Basis viewB = root.view.GlobalBasis;
            viewB.Y = Vector3.Up;

            Vector3 dir = viewB.Orthonormalized() * (new Vector3(inputs.move.X, 0f, inputs.move.Y).Normalized() * speed);
            dir.Y = vel.Y;
            vel = vel.MoveToward(dir, (float)delta * accel);

            Velocity = vel;
            MoveAndSlide();
        }
    }
}
