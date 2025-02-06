using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Hypernex.CCK;
using Hypernex.CCK.GodotVersion;
using Hypernex.CCK.GodotVersion.Classes;
using Hypernex.Tools;

namespace Hypernex.Game.Tests
{
    public partial class WorldTest : Node
    {
        [Export]
        public Node3D target;
        [Export]
        public Node3D target2;
        [Export]
        public ShapeCast3D target3;
        [Export]
        public bool save = false;
        [Export]
        public bool load = false;

        private AvatarRoot avatar;

        public override async void _Ready()
        {
            GltfSceneLoader.Init();
            GltfSceneLoader gltf = new GltfSceneLoader();
            PackedScene scn = new PackedScene();
            foreach (var item in FindChildren("*", owned: true))
            {
                item.Owner = this;
            }
            GD.PrintErr(scn.Pack(this));
            new Thread(() =>
            {
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                gltf.SaveToFile("user://test.gltf", scn);
                sw.Stop();
                GD.Print($"Save {sw.ElapsedMilliseconds}ms");
                sw.Restart();
                scn = gltf.LoadFromFile("user://test.gltf");
                sw.Stop();
                GD.Print($"Load {sw.ElapsedMilliseconds}ms");
                CallDeferred(MethodName.AddChild, scn.Instantiate());
            }).Start();
            return;
            GDLogger logger = new GDLogger();
            logger.SetLogger();
            await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
            new Thread(() =>
            {
                var root = WorldRoot.LoadFromFile("user://my_world.hnw");
                // Thread.Sleep(1000);
                CallDeferred(Node.MethodName.AddChild, root);
            }).Start();
            avatar = AvatarRoot.LoadFromFile("user://skeleton.hna");
            AddChild(avatar);
            avatar.AttachTo(target);
            // RunTest();
        }

        public override void _Process(double delta)
        {
            return;
            float s = Time.GetTicksMsec() / 1000f;
            target.Position = new Vector3(Mathf.Cos(s), 0f, Mathf.Sin(s));
            avatar.ProcessIk(true, true, target3.GlobalTransform, target3.GlobalTransform.TranslatedLocal(target3.TargetPosition * target3.GetClosestCollisionUnsafeFraction()), target2.GlobalTransform, target2.GlobalTransform);
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