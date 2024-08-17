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

        public FFGodot video;

        public override void _EnterTree()
        {
            video = new FFGodot();
            video.renderMesh = GetNode<TextureRect>(textureRect);
            video.source = GetNode<AudioStreamPlayer3D>(audioPlayer3d);
            video.Finished += OnFin;
            AddChild(video);
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
            video.Finished -= OnFin;
            video.QueueFree();
        }
    }
}
