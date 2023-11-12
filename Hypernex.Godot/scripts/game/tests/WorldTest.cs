using System.Diagnostics;
using System.Threading;
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
            // new Thread(() => RunTest()).Start();
            RunTest();
        }

        public void RunTest()
        {
            if (save)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                // WorldManager.Instance.PreSave(this);
                sw.Stop();
                GD.Print($"PreSave took {sw.ElapsedMilliseconds} ms");
                sw.Reset();
                sw.Start();
                WorldData data = WorldManager.Instance.ConvertWorld(this);
                sw.Stop();
                GD.Print($"SaveWorld took {sw.ElapsedMilliseconds} ms");
                sw.Reset();
                sw.Start();
                WorldManager.DebugSaveToFile(data, true);
                sw.Stop();
                GD.Print($"SaveToFile took {sw.ElapsedMilliseconds} ms");
            }
            if (load)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                WorldData data = WorldManager.DebugLoadFromFile();
                sw.Stop();
                GD.Print($"LoadFromFile took {sw.ElapsedMilliseconds} ms");
                sw.Reset();
                sw.Start();
                WorldRoot node = WorldManager.Instance.LoadWorld(data);
                sw.Stop();
                GD.Print($"LoadWorld took {sw.ElapsedMilliseconds} ms");
                // CallDeferred(nameof(SpawnWorld), node);
                SpawnWorld(node);
            }
        }

        public void SpawnWorld(WorldRoot world)
        {
            // WorldManager.Instance.PostLoad(world);
            AddChild(world);
            world.Load();
        }
    }
}