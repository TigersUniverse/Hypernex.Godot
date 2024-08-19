using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
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

        public override void _Ready()
        {
            Init.resolverSet = false;
            // byte[] res = await HttpUtils.HttpGetAsync(this, url);
            if (IsInstanceValid(this))
            {
                // byte[] res = FileAccess.GetFileAsBytes(url);
                FFGodot ff = new FFGodot();
                ff.CanSeek = !ImageTools.IsVideoStream(new System.Uri(url));
                ff.renderMesh = this;
                ff.source = audio;
                AddChild(ff);
                ff.Play(url, url);
                // var ff = ImageTools.LoadFFmpeg(res, this, audio);
                ff.Finished += () =>
                {
                    ff.Seek(0);
                };
                ff.Seek(0);
            }
        }
    }
}