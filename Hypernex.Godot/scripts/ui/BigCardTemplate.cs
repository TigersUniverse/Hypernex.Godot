using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using HypernexSharp.APIObjects;
using HypernexSharp.SocketObjects;

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
        public Container controlsContainer;
        [Export]
        public Container usersContainer;
        [Export]
        public AspectRatioContainer foregroundContainer;
        [Export]
        public TextureRect foreground;
        [Export]
        public VideoStreamPlayer videoForeground;
        [Export]
        public AspectRatioContainer backgroundContainer;
        [Export]
        public TextureRect background;
        [Export]
        public VideoStreamPlayer videoBackground;
        [Export]
        public PackedScene cardUI;

        public CardType type = CardType.None;
        public CardTemplate.CardUserType userType = CardTemplate.CardUserType.Other;
        public User userData = null;
        public WorldMeta worldMeta = null;
        public GameInstance gameInstance = null;
        public SafeInstance safeInstance = null;

        public override void _Ready()
        {
            videoForeground.Finished += FGLoop;
            videoBackground.Finished += BGLoop;
            usersLabel.MetaClicked += OnClick;
        }

        public override void _ExitTree()
        {
            videoForeground.Finished -= FGLoop;
            videoBackground.Finished -= BGLoop;
            usersLabel.MetaClicked -= OnClick;
        }

        private void FGLoop()
        {
            videoForeground.StreamPosition = 0;
            videoForeground.Play();
        }

        private void BGLoop()
        {
            videoBackground.StreamPosition = 0;
            videoBackground.Play();
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
            foreach (var child in usersContainer.GetChildren())
                child.QueueFree();
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
            userType = CardTemplate.CardUserType.Other;
            userData = null;
            worldMeta = null;
            gameInstance = null;
            safeInstance = null;
            foreground.Show();
            videoForeground.Stream = null;
            videoForeground.Stop();
            foregroundContainer.Ratio = 2.5f;
            background.Show();
            videoBackground.Stream = null;
            videoBackground.Stop();
            backgroundContainer.Ratio = 2.5f;
            foreach (var child in usersContainer.GetChildren())
                child.QueueFree();
            foreach (var child in controlsContainer.GetChildren())
                child.QueueFree();
            usersLabel.Text = string.Empty;
            Hide();
        }

        public void Refresh()
        {
            switch (type)
            {
                case CardType.User:
                    APITools.APIObject.GetUser(r =>
                    {
                        if (r.success)
                        {
                            APITools.RefreshUser(() =>
                            {
                                if (!IsInstanceValid(label))
                                    return;
                                SetUser(r.result.UserData, userType);
                            });
                        }
                    }, userData.Id, isUserId: true);
                    break;
            }
        }

        public void SetGameInstance(GameInstance instance)
        {
            Reset();
            Name = $"Current Instance ({instance.worldMeta.Name.Replace("[", "[lb]")})";
            type = CardType.CurrentInstance;
            gameInstance = instance;
            label.Text = instance.worldMeta.Name.Replace("[", "[lb]");
            var box1 = controlsContainer.AddVBox();
            box1.AddLabel("Actions", (ui, v) => { });
            var box2 = box1.AddHBox();
            box2.AddButton("Leave", UIButtonTheme.Warning, ui => gameInstance?.Dispose());
            DownloadTools.DownloadBytes(instance.worldMeta.ThumbnailURL, b =>
            {
                if (!IsInstanceValid(foreground))
                    return;
                Image img = ImageTools.LoadImage(b);
                if (img != null)
                    foreground.Texture = ImageTexture.CreateFromImage(img);
                else
                {
                    videoForeground.Stream = ImageTools.LoadFFmpeg(b);
                    videoForeground.Play();
                    foreground.Hide();
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
                            if (!IsInstanceValid(foreground))
                                return;
                            Image img = ImageTools.LoadImage(b);
                            if (img != null)
                                foreground.Texture = ImageTexture.CreateFromImage(img);
                            else
                            {
                                videoForeground.Stream = ImageTools.LoadFFmpeg(b);
                                videoForeground.Play();
                                foreground.Hide();
                            }
                        });
                        RefreshUsers();
                        Show();
                    });
                }
            }, instance.WorldId);
        }

        public void SetUser(User user, CardTemplate.CardUserType utype)
        {
            Reset();
            Name = "User";
            type = CardType.User;
            userType = utype;
            userData = user;
            label.Text = user.GetUsersName().Replace("[", "[lb]");
            var box1 = controlsContainer.AddVBox();
            box1.AddLabel("User Actions", (ui, v) => { });
            var box2 = box1.AddHBox();
            box2.AddButton("Invite", UIButtonTheme.Primary, ui => SocketManager.InviteUser(GameInstance.FocusedInstance, userData));
            var box3 = box2.AddVBox();
            if (APITools.CurrentUser.Following.Contains(userData.Id))
                box3.AddButton("Unfollow", UIButtonTheme.Secondary, ui =>
                {
                    ui.Text = "...";
                    ui.Disabled = true;
                    APITools.APIObject.UnfollowUser(r => Refresh(), APITools.CurrentUser, APITools.CurrentToken, userData.Id);
                });
            else
                box3.AddButton("Follow", UIButtonTheme.Secondary, ui =>
                {
                    ui.Text = "...";
                    ui.Disabled = true;
                    APITools.APIObject.FollowUser(r => Refresh(), APITools.CurrentUser, APITools.CurrentToken, userData.Id);
                });
            box3.AddButton("Remove Friend", UIButtonTheme.Danger, ui => APITools.APIObject.RemoveFriend(r => Refresh(), APITools.CurrentUser, APITools.CurrentToken, userData.Id));
            box3.AddButton("Block", UIButtonTheme.Danger, ui => APITools.APIObject.BlockUser(r => Refresh(), APITools.CurrentUser, APITools.CurrentToken, userData.Id));
            if (GameInstance.FocusedInstance != null)
            {
                var box4 = box2.AddVBox();
                box4.AddButton("Warn", UIButtonTheme.Warning, ui => GameInstance.FocusedInstance.WarnUser(userData, "TODO"));
                box4.AddButton("Kick", UIButtonTheme.Danger, ui => GameInstance.FocusedInstance.KickUser(userData, "TODO"));
                box4.AddButton("Ban", UIButtonTheme.Danger, ui => GameInstance.FocusedInstance.BanUser(userData, "TODO"));
            }
            DownloadTools.DownloadBytes(user.Bio.PfpURL, b =>
            {
                if (!IsInstanceValid(foreground))
                    return;
                foregroundContainer.Ratio = 1f;
                Image img = ImageTools.LoadImage(b);
                if (img != null)
                    foreground.Texture = ImageTexture.CreateFromImage(img);
                else
                {
                    videoForeground.Stream = ImageTools.LoadFFmpeg(b);
                    videoForeground.Play();
                    foreground.Hide();
                }
            });
            DownloadTools.DownloadBytes(user.Bio.BannerURL, b =>
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
            label.Text = "[center][font_size=24]" + world.Name.Replace("[", "[lb]") + "[/font_size][/center]";
            label.Text += "\n\n[font_size=16]" + world.Description.Replace("[", "[lb]") + "[/font_size]";
            var box1 = controlsContainer.AddVBox();
            // TODO: does this work?
            var opt1 = box1.AddOptions("Anyone", "Acquaintances", "Friends", "OpenRequest", "ModeratorRequest", "ClosedRequest");
            var opts1 = new[] { InstancePublicity.Anyone, InstancePublicity.Acquaintances, InstancePublicity.Friends, InstancePublicity.OpenRequest, InstancePublicity.ModeratorRequest, InstancePublicity.ClosedRequest };
            var opt2 = box1.AddOptions("KCP", "TCP", "UDP");
            var opts2 = new[] { InstanceProtocol.KCP, InstanceProtocol.TCP, InstanceProtocol.UDP };
            box1.AddButton("Create Instance", UIButtonTheme.Info, btn => SocketManager.CreateInstance(world, opts1[opt1.Selected], opts2[opt2.Selected]));
            DownloadTools.DownloadBytes(world.ThumbnailURL, b =>
            {
                if (!IsInstanceValid(foreground))
                    return;
                Image img = ImageTools.LoadImage(b);
                if (img != null)
                    foreground.Texture = ImageTexture.CreateFromImage(img);
                else
                {
                    videoForeground.Stream = ImageTools.LoadFFmpeg(b);
                    videoForeground.Play();
                    foreground.Hide();
                }
            });
            Show();
        }
    }
}
