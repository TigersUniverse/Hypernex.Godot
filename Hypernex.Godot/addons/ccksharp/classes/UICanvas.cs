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
        private Control keyboard;

        public override void _EnterTree()
        {
            area = new Area3D()
            {
                InputRayPickable = false,
                // InputCaptureOnDrag = true,
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
            keyboard = ResourceLoader.Load<GDScript>("res://addons/onscreenkeyboard/onscreen_keyboard.gd").New().As<Control>();
            keyboard.Set("auto_show", false);
            keyboard.Set("set_tool_tip", false);
            keyboard.Set("custom_layout_file", "res://addons/ccksharp/keyboard_layout_en.json");
            keyboard.Connect("key_pressed", Callable.From<Variant>(KeyPressed));
            keyboard.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
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
                VP.AddChild(keyboard);
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
            keyboard.QueueFree();
            area.QueueFree();
            shape.QueueFree();
            quad.QueueFree();
        }

        public void KeyPressed(Variant key)
        {
            var dict = key.AsGodotDictionary();
            if (dict.ContainsKey("func"))
            {
                switch (dict["func"].AsString())
                {
                    case "paste":
                        VP.PushTextInput(DisplayServer.ClipboardGet());
                        break;
                }
            }
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
            var prevFocus = VP.GuiGetFocusOwner();
            VP.PushInput(ev);
            var focus = VP.GuiGetFocusOwner();
            if (focus is LineEdit && (focus != prevFocus /*|| (ev is InputEventMouseButton btn && !btn.Pressed)*/))
            {
                keyboard.Call("show");
            }
        }
    }
}
