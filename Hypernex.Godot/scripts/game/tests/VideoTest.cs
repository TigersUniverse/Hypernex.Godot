using System.Diagnostics;
using System.Threading;
using Godot;
using Hypernex.CCK;
using Hypernex.Tools;

namespace Hypernex.Game.Tests
{
    public partial class VideoTest : VideoStreamPlayer
    {
        [Export]
        public string url;

        public override async void _Ready()
        {
            byte[] res = await HttpUtils.HttpGetAsync(this, url);
            if (IsInstanceValid(this))
            {
                Stream = ImageTools.LoadFFmpeg(res);
                Finished += () =>
                {
                    StreamPosition = 0;
                    Play();
                };
                Play();
            }
        }
    }
}