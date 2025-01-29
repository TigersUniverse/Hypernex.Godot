using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using FFmpeg.Godot;
using Godot;
using Hypernex.CCK;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Tools;

namespace Hypernex.Game.Tests
{
    public partial class VideoTest : TextureRect
    {
        [Export]
        public string url;
        [Export]
        public AudioStreamPlayer3D audio;
        [Export]
        public FFPlayGodot ff;
        [Export]
        public FFTexturePlayer ffTex;
        [Export]
        public FFAudioPlayer ffAud;

        public override async void _Ready()
        {
            string dir = OS.GetExecutablePath().GetBaseDir();
            if (OS.HasFeature("editor"))
            {
                dir = Path.Combine(Directory.GetCurrentDirectory(), "addons", "natives");
            }
            DynamicallyLoadedBindings.LibrariesPath = dir;
            DynamicallyLoadedBindings.Initialize();

            // Init.resolverSet = false;
            // byte[] res = await HttpUtils.HttpGetAsync(this, url);
            if (IsInstanceValid(this))
            {
                // byte[] res = FileAccess.GetFileAsBytes(url);
                // FFPlayGodot ff = new FFPlayGodot();
                // FFTexturePlayer ffTex = new FFTexturePlayer();
                // FFAudioPlayer ffAud = new FFAudioPlayer();
                // ff.texturePlayer = ffTex;
                // ff.audioPlayer = ffAud;
                // ff.AddChild(ffTex);
                // ff.AddChild(ffAud);
                ffTex.OnDisplay = OnDisplay;
                // ffAud.audioSource = audio;
                // AddChild(ff);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                ff.Play(url);
                // var ff = ImageTools.LoadFFmpeg(res, this, audio);
                ff.OnEndReached += () =>
                {
                    // ff.Seek(0);
                };
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                ff.Seek(0);
            }
        }

        private void OnDisplay(ImageTexture texture)
        {
            // GD.Print(texture);
            Texture = texture;
        }
    }
}