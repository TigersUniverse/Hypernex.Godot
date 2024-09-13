using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace Hypernex.Game
{
    public partial class Entity3D : Node3D, IEntity
    {
        [Export]
        public Array<Node> components = new Array<Node>();
        private RigidBody3D rb;

        public bool Enabled
        {
            get => CanProcess();
            set
            {
                Visible = value;
                ProcessMode = value ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
            }
        }

        public Node GetComponent(Type type)
        {
            return components.FirstOrDefault(x => x.GetType().IsAssignableTo(type));
        }

        public Node[] GetComponents(Type type)
        {
            return components.Where(x => x.GetType().IsAssignableTo(type)).ToArray();
        }

        public Node AddComponent(Node value)
        {
            components.Add(value);
            value.SetMeta(IEntity.TypeName, this);
            return value;
        }

        public override void _EnterTree()
        {
            foreach (var comp in components)
            {
                comp.SetMeta(IEntity.TypeName, this);
            }
        }

        public override void _Ready()
        {
            rb = this.GetComponent<RigidBody3D>();
            rb.TopLevel = true;
            rb.GlobalTransform = GlobalTransform;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsInstanceValid(rb))
            {
                GlobalTransform = rb.GlobalTransform;
            }
        }
    }
}