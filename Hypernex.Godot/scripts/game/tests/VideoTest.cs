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

        [Export]
        public HSlider slider;

        public override async void _Ready()
        {
            new GDLogger().SetLogger();
            Init.UpdateYtDl();

            /*
            string dir = OS.GetExecutablePath().GetBaseDir();
            if (OS.HasFeature("editor"))
            {
                dir = Path.Combine(Directory.GetCurrentDirectory(), "export_data", OS.GetName().ToLower());
            }
            DynamicallyLoadedBindings.LibrariesPath = dir;
            DynamicallyLoadedBindings.Initialize();
            */

            ffTex.OnDisplay += OnDisplay;
            slider.ValueChanged += Seek;

            var video = await Streaming.ytdl.RunVideoDownload(url, "bestvideo[height<=?1080]/best");
            var audio = await Streaming.ytdl.RunAudioDownload(url);
            ff.Play(video.Data, audio.Data);
            ff.OnEndReached += () =>
            {
                ff.Seek(0);
            };
            ff.Seek(0);
            slider.MaxValue = ff.GetLength();
        }

        public override void _Process(double delta)
        {
            slider.SetValueNoSignal(ff.PlaybackTime);
        }

        private void Seek(double value)
        {
            ff.Seek(value);
        }

        private void OnDisplay(ImageTexture texture)
        {
            Texture = texture;
        }
    }
}