using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Hypernex.CCK.GodotVersion;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.Tools;

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
            await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
            var root = WorldRoot.LoadFromFile("user://my_world.hnw");
            AddChild(root);
            // RunTest();
        }

        public void RunTest()
        {
            if (save)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                string dir = Path.Combine(OS.GetUserDataDir(), "WorldSaves", "test.hnw");
                WorldRoot.SaveToFile(dir, this);
                sw.Stop();
                GD.Print($"SaveToFile took {sw.ElapsedMilliseconds} ms");
            }
            if (load)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                string dir = Path.Combine(OS.GetUserDataDir(), "WorldSaves", "test.hnw");
                WorldRoot root = WorldRoot.LoadFromFile(dir);
                sw.Stop();
                GD.Print($"LoadFromFile took {sw.ElapsedMilliseconds} ms");
                AddChild(root);
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