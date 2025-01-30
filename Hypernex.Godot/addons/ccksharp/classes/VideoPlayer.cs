using System;
using FFmpeg.Godot;
using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    public partial class VideoPlayer : Node, ISandboxClass
    {
        public const string TypeName = "VideoPlayer";

        [Export]
        public NodePath textureRect { get; set; }
        [Export]
        public NodePath audioPlayer3d { get; set; }
        [Export]
        public bool loop { get; set; } = false;

        public FFPlayGodot video;
        public FFTexturePlayer texture;
        public FFAudioPlayer audio;

        public override void _EnterTree()
        {
            video = new FFPlayGodot();
            texture = new FFTexturePlayer();
            audio = new FFAudioPlayer();
            video.texturePlayer = texture;
            video.audioPlayer = audio;
            video.AddChild(texture);
            video.AddChild(audio);
            // if (OS.GetName().Equals("Android", StringComparison.OrdinalIgnoreCase))
                // video._hwType = AVHWDeviceType.AV_HWDEVICE_TYPE_MEDIACODEC;
            texture.OnDisplay = OnDisplay;
            audio.audioSource = GetNode<AudioStreamPlayer3D>(audioPlayer3d);
            video.OnEndReached += OnFin;
            AddChild(video);
        }

        private void OnDisplay(ImageTexture tex)
        {
            GetNode<TextureRect>(textureRect).Texture = tex;
        }

        private void OnFin()
        {
            if (loop)
            {
                video.Seek(0d);
            }
        }

        public override void _ExitTree()
        {
            video.OnEndReached -= OnFin;
            video.QueueFree();
        }
    }
}
