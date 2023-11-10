using Godot;

namespace Hypernex.Tools.Godot
{
    public partial class DiscordGDTools : Node
    {
        public override void _Ready()
        {
            DiscordTools.StartDiscord();
        }

        public override void _PhysicsProcess(double delta)
        {
            DiscordTools.RunCallbacks();
        }

        public override void _ExitTree()
        {
            DiscordTools.Stop();
        }
    }
}
