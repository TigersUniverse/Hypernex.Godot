using System;
using System.Linq;
using Godot;
using Hypernex.Tools;
using HypernexSharp.APIObjects;

namespace Hypernex.UI
{
    public partial class BigCardTemplate : Control
    {
        public enum CardType
        {
            None,
            User,
            World,
            Instance,
            CurrentInstance,
        }

        [Export(PropertyHint.MultilineText)]
        public string usersLabelFormat = "[center]Users ({0})[/center][right][url]Refresh[/url][/right]";
        [Export]
        public RichTextLabel label;
        [Export]
        public RichTextLabel usersLabel;
        [Export]
        public Container usersContainer;
        [Export]
        public TextureRect background;
        [Export]
        public VideoStreamPlayer videoBackground;
        [Export]
        public PackedScene cardUI;

        public CardType type = CardType.None;
        public User userData = null;
        public WorldMeta worldMeta = null;
        public GameInstance gameInstance = null;
        public SafeInstance safeInstance = null;

        public override void _Ready()
        {
            usersLabel.MetaClicked += OnClick;
        }

        public override void _ExitTree()
        {
            usersLabel.MetaClicked -= OnClick;
        }

        private void OnClick(Variant meta)
        {
            switch (meta.AsString().ToLower())
            {
                default:
                    usersLabel.Text = string.Empty;
                    RefreshUsers();
                    break;
                case "leave":
                    gameInstance?.Dispose();
                    break;
            }
        }

        public void RefreshUsers()
        {
            if (type != CardType.Instance && type != CardType.CurrentInstance)
                return;
            string[] users = Array.Empty<string>();
            if (safeInstance != null)
            {
                users = safeInstance.ConnectedUsers.ToArray();
            }
            else if (gameInstance != null)
            {
                users = gameInstance.ConnectedUsers.Select(x => x.Id).ToArray();
            }
            usersLabel.Text = string.Format(usersLabelFormat, users.Length);
            foreach (var user in users)
            {
                var node = cardUI.Instantiate<CardTemplate>();
                usersContainer.AddChild(node);
                node.SetUserId(user, CardTemplate.CardUserType.Instance);
            }
        }

        public void Reset()
        {
            type = CardType.None;
            userData = null;
            worldMeta = null;
            gameInstance = null;
            safeInstance = null;
            background.Show();
            videoBackground.Stream = null;
            videoBackground.Stop();
            foreach (var child in usersContainer.GetChildren())
                child.QueueFree();
            usersLabel.Text = string.Empty;
            Hide();
        }

        public void SetGameInstance(GameInstance instance)
        {
            Reset();
            Name = $"Current Instance ({instance.worldMeta.Name.Replace("[", "[lb]")})";
            type = CardType.CurrentInstance;
            gameInstance = instance;
            label.Text = instance.worldMeta.Name.Replace("[", "[lb]");
            DownloadTools.DownloadBytes(instance.worldMeta.ThumbnailURL, b =>
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
            RefreshUsers();
            Show();
        }

        public void SetSafeInstance(SafeInstance instance)
        {
            Reset();
            Name = "Instance (...)";
            APITools.APIObject.GetWorldMeta(r =>
            {
                if (r.success)
                {
                    QuickInvoke.InvokeActionOnMainThread(() =>
                    {
                        if (!IsInstanceValid(label))
                            return;
                        Name = $"Instance ({r.result.Meta.Name.Replace("[", "[lb]")})";
                        type = CardType.Instance;
                        safeInstance = instance;
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
                        RefreshUsers();
                        Show();
                    });
                }
            }, instance.WorldId);
        }

        public void SetUser(User user)
        {
            Reset();
            Name = "User";
            type = CardType.User;
            userData = user;
            label.Text = user.GetUsersName().Replace("[", "[lb]");
            DownloadTools.DownloadBytes(user.Bio.PfpURL, b =>
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
            Show();
        }

        public void SetWorldMeta(WorldMeta world)
        {
            Reset();
            Name = "World";
            type = CardType.World;
            worldMeta = world;
            label.Text = world.Name.Replace("[", "[lb]");
            DownloadTools.DownloadBytes(world.ThumbnailURL, b =>
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
            Show();
        }
    }
}