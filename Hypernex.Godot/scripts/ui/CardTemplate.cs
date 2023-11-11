using System;
using System.IO;
using Godot;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using HypernexSharp.SocketObjects;

namespace Hypernex.UI
{
    public partial class CardTemplate : Control
    {
        public enum CardType
        {
            None,
            User,
            World,
        }

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
        [Export]
        public Button button;
        public CardType type = CardType.None;
        public string cardInfoId = string.Empty;
        public WorldMeta worldMeta = null;

        public override void _Ready()
        {
            videoIcon.Finished += IconVidDone;
            videoBackground.Finished += VidDone;
            button.Pressed += Clicked;
        }

        public override void _ExitTree()
        {
            videoIcon.Stream = null;
            videoBackground.Stream = null;
            videoIcon.Finished -= IconVidDone;
            videoBackground.Finished -= VidDone;
            button.Pressed -= Clicked;
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

        private void Clicked()
        {
            switch (type)
            {
                case CardType.User:
                    break;
                case CardType.World:
                    SocketManager.CreateInstance(worldMeta, InstancePublicity.ClosedRequest, InstanceProtocol.KCP);
                    break;
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

        public void Reset()
        {
            cardInfoId = string.Empty;
            worldMeta = null;
            icon.Show();
            videoIcon.Stream = null;
            videoIcon.Stop();
            background.Show();
            videoBackground.Stream = null;
            videoBackground.Stop();
        }

        public void SetWorldId(string worldId)
        {
            Reset();
            APITools.APIObject.GetWorldMeta(r =>
            {
                if (r.success)
                {
                    QuickInvoke.InvokeActionOnMainThread(() =>
                    {
                        if (!IsInstanceValid(label))
                            return;
                        type = CardType.World;
                        cardInfoId = worldId;
                        worldMeta = r.result.Meta;
                        label.Text = r.result.Meta.Name.Replace("[", "[lb]");
                        DownloadTools.DownloadBytes(r.result.Meta.ThumbnailURL, b =>
                        {
                            if (!IsInstanceValid(background))
                                return;
                            Image img = ImageTools.LoadImage(b);
                            if (img != null)
                                background.Texture = ImageTexture.CreateFromImage(img);
                            else
                            {
                                videoBackground.Stream = ImageTools.LoadFFmpeg(b);
                                videoBackground.Play();
                                background.Hide();
                            }
                        });
                    });
                }
            }, worldId);
        }

        public void SetUserId(string userId)
        {
            Reset();
            APITools.APIObject.GetUser(r =>
            {
                if (r.success)
                {
                    QuickInvoke.InvokeActionOnMainThread(() =>
                    {
                        if (!IsInstanceValid(label))
                            return;
                        type = CardType.User;
                        cardInfoId = userId;
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
                                videoIcon.Stream = ImageTools.LoadFFmpeg(b);
                                videoIcon.Play();
                                icon.Hide();
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
                                videoBackground.Stream = ImageTools.LoadFFmpeg(b);
                                videoBackground.Play();
                                background.Hide();
                            }
                        });
                    });
                }
            }, userId, isUserId: true);
        }
    }
}
