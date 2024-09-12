using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    public partial class NetworkSyncDescriptor : Node, ISandboxClass
    {
        [Export]
        public bool InstanceHostOnly = false;
        [Export]
        public bool CanSteal = false;
        [Export]
        public bool AlwaysSync = false;
    }
}