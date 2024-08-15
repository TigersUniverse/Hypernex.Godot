using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Godot;
using Hypernex.CCK;
using Hypernex.Tools;

namespace Hypernex.Game.Tests
{
    public partial class VideoTest : TextureRect
    {
        [Export]
        public string url;
        [Export]
        public AudioStreamPlayer3D audio;

        public override async void _Ready()
        {
            Init.resolverSet = false;
            byte[] res = await HttpUtils.HttpGetAsync(this, url);
            if (IsInstanceValid(this))
            {
                var ff = ImageTools.LoadFFmpeg(res, this, audio);
                ff.Finished += () =>
                {
                    ff.Seek(0);
                };
                ff.Seek(0);
            }
        }
    }
}