using System.Diagnostics;
using System.Threading;
using Godot;

namespace Hypernex.Game.Tests
{
    public partial class VideoTest : VideoStreamPlayer
    {
        [Export]
        public string url;

        public override void _Ready()
        {
            Stream = ResourceLoader.Load(url) as VideoStream;
            Finished += () =>
            {
                StreamPosition = 0;
                Play();
            };
            Play();
        }
    }
}