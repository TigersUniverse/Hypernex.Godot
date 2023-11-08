using System;
using System.IO;
using Godot;
using Hypernex.Tools;

namespace Hypernex.UI
{
    public partial class CardTemplate : Control
    {
        [Export]
        public TextureRect icon;
        [Export]
        public TextureRect background;
        [Export]
        public RichTextLabel label;
        [Export]
        public VideoStreamPlayer videoBackground;

        public override void _Ready()
        {
            videoBackground.Finished += VidDone;
        }

        public override void _ExitTree()
        {
            videoBackground.Finished -= VidDone;
        }

        private void VidDone()
        {
            videoBackground.StreamPosition = 0;
            videoBackground.Play();
        }

        public override void _Process(double delta)
        {
            return;
            if (videoBackground.Stream != null && !videoBackground.IsPlaying())
            {
                videoBackground.Play();
            }
        }

        public void SetUserId(string userid)
        {
            background.Show();
            videoBackground.Stream = null;
            videoBackground.Stop();
            Init.Instance.hypernex.GetUser(r =>
            {
                if (r.success)
                {
                    QuickInvoke.InvokeActionOnMainThread(() =>
                    {
                        label.Text = r.result.UserData.Username;
                        DownloadTools.DownloadBytes(r.result.UserData.Bio.PfpURL, b =>
                        {
                            Image img = ImageTools.LoadImage(b);
                            if (img != null)
                                icon.Texture = ImageTexture.CreateFromImage(img);
                        });
                        DownloadTools.DownloadBytes(r.result.UserData.Bio.BannerURL, b =>
                        {
                            Image img = ImageTools.LoadImage(b);
                            if (img != null)
                                background.Texture = ImageTexture.CreateFromImage(img);
                            else
                            {
                                string path = Path.Combine(DownloadTools.DownloadsPath, Path.GetFileName(Path.GetTempFileName()) + ".gif");
                                if (!Directory.Exists(DownloadTools.DownloadsPath))
                                    Directory.CreateDirectory(DownloadTools.DownloadsPath);
                                File.WriteAllBytes(path, b);
                                var asset = ClassDB.Instantiate("FFmpegVideoStream").AsGodotObject();
                                if (asset is VideoStream vid)
                                {
                                    vid.File = path;
                                    videoBackground.Stream = vid;
                                    videoBackground.Play();
                                    background.Hide();
                                }
                            }
                        });
                    });
                }
            }, userid, isUserId: true);
        }
    }
}
