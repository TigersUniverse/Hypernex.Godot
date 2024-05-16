using System.Diagnostics;
using System.IO;
using Godot;
using Godot.Collections;
using Hypernex.CCK.GodotVersion;
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
            // Dictionary dict = new Dictionary();
            // dict.Add("test", this);
            // GD.Print(GD.VarToStr(dict));
            // SaveLoader.ParseProperty("hello = \"world!\"", 0, out var key, out var val, out var off);
            // GD.PrintS(key, val, off);

            SaveLoader.ParsedTres tscn = SaveLoader.ParseTres(string.Empty, File.ReadAllText(ProjectSettings.GlobalizePath("user://test.tres")).ReplaceLineEndings("\n"));
            GD.Print(JsonTools.JsonSerialize(tscn));

            var loader = new SaveLoader();
            loader.ReadZip("res://temp/bin.zip");
            AddChild(loader.scene.Instantiate());
            await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
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