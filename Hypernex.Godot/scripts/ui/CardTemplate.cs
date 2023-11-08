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
        public VideoStreamPlayer videoIcon;
        [Export]
        public VideoStreamPlayer videoBackground;

        public override void _Ready()
        {
            videoIcon.Finished += IconVidDone;
            videoBackground.Finished += VidDone;
        }

        public override void _ExitTree()
        {
            videoIcon.Finished -= IconVidDone;
            videoBackground.Finished -= VidDone;
        }

        private void IconVidDone()
        {
            videoIcon.StreamPosition = 0;
            videoIcon.Play();
        }

        private void VidDone()
        {
            videoBackground.StreamPosition = 0;
            videoBackground.Play();
        }

        public void SetUserId(string userid)
        {
            icon.Show();
            videoIcon.Stream = null;
            videoIcon.Stop();
            background.Show();
            videoBackground.Stream = null;
            videoBackground.Stop();
            Init.Instance.hypernex.GetUser(r =>
            {
                if (r.success)
                {
                    QuickInvoke.InvokeActionOnMainThread(() =>
                    {
                        if (!IsInstanceValid(label))
                            return;
                        label.Text = r.result.UserData.Username;
                        DownloadTools.DownloadBytes(r.result.UserData.Bio.PfpURL, b =>
                        {
                            if (!IsInstanceValid(icon))
                                return;
                            Image img = ImageTools.LoadImage(b);
                            if (img != null)
                                icon.Texture = ImageTexture.CreateFromImage(img);
                            else
                            {
                                string path = Path.GetTempFileName();
                                File.WriteAllBytes(path, b);
                                var asset = ClassDB.Instantiate("FFmpegVideoStream").AsGodotObject();
                                if (asset is VideoStream vid)
                                {
                                    vid.File = path;
                                    videoIcon.Stream = vid;
                                    videoIcon.Play();
                                    icon.Hide();
                                }
                            }
                        });
                        DownloadTools.DownloadBytes(r.result.UserData.Bio.BannerURL, b =>
                        {
                            if (!IsInstanceValid(background))
                                return;
                            Image img = ImageTools.LoadImage(b);
                            if (img != null)
                                background.Texture = ImageTexture.CreateFromImage(img);
                            else
                            {
                                string path = Path.GetTempFileName();
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
