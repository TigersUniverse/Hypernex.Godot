using System;
using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    public partial class ReverbZone : CsgBox3D, ISandboxClass
    {
        public const string TypeName = "ReverbZone";

        [Export]
        public AudioEffect effect;

        private int BusIndex
        {
            get
            {
                for (int i = 0; i < AudioServer.BusCount; i++)
                {
                    if (AudioServer.GetBusName(i) == BusName)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }
        private string BusName;

        private Area3D area;
        private CollisionShape3D collision;

        public override void _EnterTree()
        {
            BusName = Guid.NewGuid().ToString();
            AudioServer.AddBus();
            AudioServer.SetBusName(AudioServer.BusCount - 1, BusName);
            AudioServer.SetBusSend(BusIndex, "World");
            AudioServer.AddBusEffect(BusIndex, effect);
            area = new Area3D();
            collision = new CollisionShape3D();
            collision.Shape = new BoxShape3D()
            {
                Size = Size,
            };
            area.AddChild(collision);
            AddChild(area);
            area.ReverbBusEnabled = true;
            area.ReverbBusAmount = 1f;
            area.ReverbBusName = BusName;
        }

        public override void _ExitTree()
        {
            for (int i = 0; i < AudioServer.GetBusEffectCount(BusIndex); i++)
            {
                if (AudioServer.GetBusEffect(BusIndex, i) == effect)
                {
                    AudioServer.RemoveBusEffect(BusIndex, i);
                    break;
                }
            }
            AudioServer.RemoveBus(BusIndex);
        }

        public override void _Process(double delta)
        {
            Visible = false;
            /*
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
            */
        }
    }
}
