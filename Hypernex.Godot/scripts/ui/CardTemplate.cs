using System;
using System.IO;
using Godot;
using Hypernex.Tools;
using HypernexSharp.APIObjects;

namespace Hypernex.UI
{
    public partial class CardTemplate : Control
    {
        public static Vector2 baseSize = new Vector2(1280, 720);
        [Export]
        public Vector2 scaleSize = new Vector2(250, 100);
        [Export]
        public bool scaleWithScreen = false;
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
            videoIcon.Stream = null;
            videoBackground.Stream = null;
            videoIcon.Finished -= IconVidDone;
            videoBackground.Finished -= VidDone;
        }

        public override void _Process(double delta)
        {
            if (scaleWithScreen)
            {
                var rect = GetViewportRect();
                var ratio = scaleSize / baseSize;
                var baseRatio = baseSize.X / baseSize.Y;
                var size = new Vector2(rect.Size.Y * baseRatio, rect.Size.Y);
                CustomMinimumSize = size * ratio;
            }
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

        public static string GetColor(Status status)
        {
            switch (status)
            {
                case Status.Offline:
                    return Colors.Gray.ToHtml();
                case Status.Online:
                    return Colors.Green.ToHtml();
                case Status.Absent:
                    return Colors.Yellow.ToHtml();
                case Status.Party:
                    return Colors.Cyan.ToHtml();
                case Status.DoNotDisturb:
                    return Colors.Red.ToHtml();
                default:
                    return Colors.White.ToHtml();
            }
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
                        label.Text = $"{r.result.UserData.Username.Replace("[", "[lb]")} [color={GetColor(r.result.UserData.Bio.Status)}]{r.result.UserData.Bio.Status}[/color]";
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
