using System;
using FFmpeg.Godot;
using Godot;

namespace Hypernex.CCK.GodotVersion.Classes
{
    [Tool]
    [GlobalClass]
    public partial class VideoPlayer : Node3D, ISandboxClass
    {
        public const string TypeName = "VideoPlayer";

        [Export]
        public NodePath VideoPlayback { get; set; }
        [Export]
        public NodePath AudioPlayback { get; set; }

        public bool Loop { get; set; } = false;

        public FFPlayGodot video;
        public FFTexturePlayer texture;
        public FFAudioPlayer audio;

        public override void _EnterTree()
        {
            if (Engine.IsEditorHint())
                return;
            video = new FFPlayGodot();
            texture = new FFTexturePlayer();
            audio = new FFAudioPlayer();
            video.texturePlayer = texture;
            video.audioPlayer = audio;
            video.AddChild(texture);
            video.AddChild(audio);
            // if (OS.GetName().Equals("Android", StringComparison.OrdinalIgnoreCase))
                // video._hwType = AVHWDeviceType.AV_HWDEVICE_TYPE_MEDIACODEC;
            texture.OnDisplay += OnDisplay;
            audio.audioSource = GetNodeOrNull<AudioStreamPlayer3D>(AudioPlayback);
            video.OnEndReached += OnFin;
            AddChild(video);
        }

        private void OnDisplay(ImageTexture tex)
        {
            var texRect = GetNodeOrNull<TextureRect>(VideoPlayback);
            var meshInst = GetNodeOrNull<MeshInstance3D>(VideoPlayback);
            if (IsInstanceValid(texRect))
            {
                texRect.Texture = tex;
            }
            else if (IsInstanceValid(meshInst))
            {
                switch (meshInst.MaterialOverride)
                {
                    case BaseMaterial3D mat3d:
                        mat3d.AlbedoTexture = tex;
                        if (mat3d.EmissionEnabled)
                            mat3d.EmissionTexture = tex;
                        break;
                    case ShaderMaterial shaderMat:
                        // TODO
                        break;
                }
            }
        }

        private void OnFin()
        {
            if (Loop)
            {
                video.Seek(0d);
            }
        }

        public override void _ExitTree()
        {
            if (Engine.IsEditorHint())
                return;
            video.OnEndReached -= OnFin;
            video.QueueFree();
        }
    }
}
