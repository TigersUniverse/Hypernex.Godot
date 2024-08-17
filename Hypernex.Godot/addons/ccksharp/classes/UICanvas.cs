using System;
using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    public partial class UICanvas : Node3D, ISandboxClass
    {
        public const string TypeName = "UICanvas";

        [Export]
        public Vector2 size = Vector2.One;
        [Export]
        public NodePath subViewport;
        public SubViewport VP => GetNode<SubViewport>(subViewport);
        [Export]
        public Material material;

        private Area3D area;
        private CollisionShape3D shape;
        private MeshInstance3D quad;

        public override void _EnterTree()
        {
            area = new Area3D()
            {
                InputCaptureOnDrag = true,
            };
            shape = new CollisionShape3D()
            {
                Shape = new BoxShape3D()
                {
                    Size = new Vector3(size.X, size.Y, 0.1f),
                },
            };
            quad = new MeshInstance3D()
            {
                Mesh = new QuadMesh()
                {
                    Size = size,
                },
            };
            if (IsInstanceValid(VP))
            {
                VP.GuiEmbedSubwindows = true;
                VP.Disable3D = true;
                VP.HandleInputLocally = true;
                VP.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
                quad.MaterialOverride = new StandardMaterial3D()
                {
                    AlbedoTexture = VP.GetTexture(),
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                };
            }
            if (IsInstanceValid(material))
            {
                quad.MaterialOverride = material;
            }
            area.InputEvent += HandleInput;
            area.SetMeta(TypeName, this);
            area.AddChild(shape);
            quad.AddChild(area);
            AddChild(quad);
        }

        public override void _ExitTree()
        {
            area.QueueFree();
            shape.QueueFree();
            quad.QueueFree();
        }

        public void HandleInput(Node camera, InputEvent ev, Vector3 eventPosition, Vector3 normal, long shapeIdx)
        {
            if (!IsInstanceValid(VP))
                return;
            eventPosition = quad.GlobalTransform.AffineInverse() * eventPosition;
            Vector2 pos = new Vector2(eventPosition.X, -eventPosition.Y);
            pos /= size;
            pos += Vector2.One * 0.5f;
            pos *= VP.Size;
            if (ev is InputEventMouse evMouse)
            {
                evMouse.GlobalPosition = pos;
                evMouse.Position = pos;
            }
            VP.PushInput(ev);
        }
    }
}
