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

        string IEntity.Name
        {
            get => Name;
            set => Name = value;
        }

        public IEntity ParentEnt => GetParent() is IEntity ent ? ent : null;

        public Node GetComponent(Type type)
        {
            return components.FirstOrDefault(x => type.IsAssignableFrom(x.GetType()));
        }

        public Node[] GetComponents(Type type)
        {
            return components.Where(x => type.IsAssignableFrom(x.GetType())).ToArray();
        }

        public Node AddComponent(Node value)
        {
            components.Add(value);
            value.SetMeta(IEntity.TypeName, value.GetPathTo(this));
            return value;
        }

        public IEntity[] GetChildEnts()
        {
            return GetChildren().Where(x => x is IEntity).Select(x => x as IEntity).ToArray();
        }

        public override void _EnterTree()
        {
            foreach (var comp in components)
            {
                comp.SetMeta(IEntity.TypeName, comp.GetPathTo(this));
            }
        }

        public override void _Ready()
        {
            rb = this.GetComponent<RigidBody3D>();
            if (IsInstanceValid(rb))
            {
                rb.TopLevel = true;
                rb.GlobalTransform = GlobalTransform;
            }
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