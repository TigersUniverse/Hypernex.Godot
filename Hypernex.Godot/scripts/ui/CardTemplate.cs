using System;
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

        public void SetUserId(string userid)
        {
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
                        });
                    });
                }
            }, userid, isUserId: true);
        }
    }
}
