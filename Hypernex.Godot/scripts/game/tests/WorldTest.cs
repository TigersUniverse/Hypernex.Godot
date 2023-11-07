using Godot;

namespace Hypernex.Game.Tests
{
    public partial class WorldTest : Node
    {
        [Export]
        public bool save = false;
        [Export]
        public bool load = false;

        public override async void _Ready()
        {
            await ToSignal(GetTree().CreateTimer(0.25f), "timeout");
            if (save)
                WorldManager.SaveToFile(WorldManager.Instance.SaveWorld(this));
            else
                AddChild(WorldManager.Instance.LoadWorld(WorldManager.LoadFromFile()));
        }
    }
}