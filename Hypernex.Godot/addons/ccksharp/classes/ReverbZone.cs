using System;
using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    public partial class ReverbZone : CsgBox3D, ISandboxClass
    {
        public const string TypeName = "ReverbZone";

        [Export]
        public AudioEffect effect;

        private int bus = 0;

        public override void _EnterTree()
        {
            AudioServer.AddBusEffect(bus, effect);
        }

        public override void _ExitTree()
        {
            for (int i = 0; i < AudioServer.GetBusEffectCount(bus); i++)
            {
                if (AudioServer.GetBusEffect(bus, i) == effect)
                {
                    AudioServer.RemoveBusEffect(bus, i);
                    break;
                }
            }
        }

        public override void _Process(double delta)
        {
            Visible = false;
            Vector3 size = Size;
            Aabb aabb = new Aabb(size / -2f, size);
            for (int i = 0; i < AudioServer.GetBusEffectCount(bus); i++)
            {
                if (AudioServer.GetBusEffect(bus, i) == effect)
                {
                    AudioServer.SetBusEffectEnabled(bus, i, (GlobalTransform * aabb).HasPoint(GetViewport().GetCamera3D().GlobalPosition));
                    break;
                }
            }
        }
    }
}
