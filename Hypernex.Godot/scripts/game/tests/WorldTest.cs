using System.Diagnostics;
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
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                WorldData data = WorldManager.Instance.SaveWorld(this);
                sw.Stop();
                GD.Print($"SaveWorld took {sw.ElapsedMilliseconds} ms");
                sw.Start();
                WorldManager.SaveToFile(data);
                sw.Stop();
                GD.Print($"SaveToFile took {sw.ElapsedMilliseconds} ms");
            }
            if (load)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                WorldData data = WorldManager.LoadFromFile();
                sw.Stop();
                GD.Print($"LoadFromFile took {sw.ElapsedMilliseconds} ms");
                sw.Start();
                Node node = WorldManager.Instance.LoadWorld(data);
                sw.Stop();
                GD.Print($"LoadWorld took {sw.ElapsedMilliseconds} ms");
                AddChild(node);
            }
        }
    }
}