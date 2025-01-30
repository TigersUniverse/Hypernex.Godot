using System;
using System.IO;
using FFmpeg.Godot;
using Godot;
using Hypernex.Configuration;
using Hypernex.Game;
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
            Instance,
            Avatar,
        }

        public enum CardUserType
        {
            Other,
            Friend,
            FriendRequest,
            Instance,
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
        public FFPlayGodot videoIcon;
        [Export]
        public FFPlayGodot videoBackground;
        [Export]
        public Button button;
        [Export]
        public MenuButton menu;

        public CardType type = CardType.None;
        public string cardInfoId = string.Empty;
        public CardUserType userType = CardUserType.Other;
        public User userData = null;
        public WorldMeta worldMeta = null;
        public AvatarMeta avatarMeta = null;
        public SafeInstance safeInstance = null;

        public bool isLoaded = false;
        public bool shouldShow = false;

        public override void _EnterTree()
        {
            videoIcon.OnEndReached += IconVidDone;
            videoBackground.OnEndReached += VidDone;
            if (button != null)
                button.Pressed += Clicked;
            if (menu != null)
                menu.AboutToPopup += MenuClicked;
            if (menu != null)
                menu.GetPopup().IndexPressed += MenuSelected;
        }

        public override void _ExitTree()
        {
            videoIcon.Pause();
            videoBackground.Pause();
            videoIcon.OnEndReached -= IconVidDone;
            videoBackground.OnEndReached -= VidDone;
            if (button != null)
                button.Pressed -= Clicked;
            if (menu != null)
                menu.AboutToPopup -= MenuClicked;
            if (menu != null)
                menu.GetPopup().IndexPressed -= MenuSelected;
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

        private void MenuSelected(long index)
        {
            var popup = menu.GetPopup();
            var id = popup.GetItemId((int)index);
            switch (type)
            {
                case CardType.User:
                    switch (id)
                    {
                        case 1:
                            SocketManager.InviteUser(GameInstance.FocusedInstance, userData);
                            break;
                        case 2:
                            APITools.APIObject.AcceptFriendRequest(r => { }, APITools.CurrentUser, APITools.CurrentToken, userData.Id);
                            break;
                        case 3:
                            APITools.APIObject.DeclineFriendRequest(r => { }, APITools.CurrentUser, APITools.CurrentToken, userData.Id);
                            break;
                        case 4:
                            APITools.APIObject.SendFriendRequest(r => { }, APITools.CurrentUser, APITools.CurrentToken, userData.Id);
                            break;
                        case 5:
                            Init.Instance.overlay.AddCard(userData, userType);
                            break;
                    }
                    break;
                case CardType.World:
                    switch (id)
                    {
                        case 1:
                            Init.Instance.overlay.AddCard(worldMeta);
                            break;
                        case 2:
                            SocketManager.CreateInstance(worldMeta, InstancePublicity.Friends, InstanceProtocol.KCP);
                            break;
                        case 3:
                            SocketManager.CreateInstance(worldMeta, InstancePublicity.Anyone, InstanceProtocol.KCP);
                            break;
                        case 4:
                            SocketManager.CreateInstance(worldMeta, InstancePublicity.ClosedRequest, InstanceProtocol.KCP);
                            break;
                    }
                    break;
                case CardType.Instance:
                    switch (id)
                    {
                        case 1:
                            Init.Instance.overlay.AddCard(safeInstance);
                            break;
                        case 2:
                            SocketManager.JoinInstance(safeInstance);
                            break;
                    }
                    break;
                case CardType.Avatar:
                    switch (id)
                    {
                        case 1:
                            ConfigManager.SelectedConfigUser.CurrentAvatar = avatarMeta.Id;
                            ConfigManager.SaveConfigToFile();
                            if (IsInstanceValid(PlayerRoot.Local))
                                PlayerRoot.Local.ChangeAvatar(avatarMeta.Id);
                            break;
                    }
                    break;
            }
        }

        private void MenuClicked()
        {
            var popup = menu.GetPopup();
            popup.Clear();
            switch (type)
            {
                case CardType.User:
                    switch (userType)
                    {
                        default:
                            popup.AddItem("View User", 5);
                            break;
                        case CardUserType.Friend:
                            popup.AddItem("View User", 5);
                            popup.AddItem("Invite", 1);
                            break;
                        case CardUserType.FriendRequest:
                            popup.AddItem("View User", 5);
                            popup.AddItem("Accept", 2);
                            popup.AddItem("Decline", 3);
                            break;
                        case CardUserType.Instance:
                            popup.AddItem("View User", 5);
                            popup.AddItem("Send Friend Request", 4);
                            break;
                    }
                    break;
                case CardType.World:
                    popup.AddItem("View World", 1);
                    popup.AddItem("Create Instance (Friends)", 2);
                    popup.AddItem("Create Instance (Anyone)", 3);
                    popup.AddItem("Create Instance (Closed Request)", 4);
                    break;
                case CardType.Instance:
                    popup.AddItem("View Instance", 1);
                    popup.AddItem("Join Instance", 2);
                    break;
                case CardType.Avatar:
                    popup.AddItem("Equip Avatar", 1);
                    break;
            }
        }

        private void Clicked()
        {
            switch (type)
            {
                case CardType.User:
                    break;
                case CardType.World:
                    SocketManager.CreateInstance(worldMeta, InstancePublicity.Friends, InstanceProtocol.KCP);
                    break;
                case CardType.Instance:
                    SocketManager.JoinInstance(safeInstance);
                    break;
                case CardType.Avatar:
                    ConfigManager.SelectedConfigUser.CurrentAvatar = avatarMeta.Id;
                    ConfigManager.SaveConfigToFile();
                    if (IsInstanceValid(PlayerRoot.Local))
                        PlayerRoot.Local.ChangeAvatar(avatarMeta.Id);
                    break;
            }
        }

        private void IconVidDone()
        {
            videoIcon.Seek(0);
        }

        private void VidDone()
        {
            videoBackground.Seek(0);
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
            type = CardType.None;
            cardInfoId = string.Empty;
            userType = CardUserType.Other;
            userData = null;
            worldMeta = null;
            avatarMeta = null;
            safeInstance = null;
            icon.Show();
            videoIcon.Pause();
            background.Show();
            videoBackground.Pause();
            if (menu != null)
                menu.GetPopup().Clear();
            Hide();
            isLoaded = false;
            shouldShow = false;
            // Show();
        }

        public void SetSafeInstance(SafeInstance instance)
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
                        type = CardType.Instance;
                        cardInfoId = instance.InstanceId;
                        safeInstance = instance;
                        worldMeta = r.result.Meta;
                        label.Text = $"{r.result.Meta.Name.Replace("[", "[lb]")} ({instance.ConnectedUsers.Count} Users)";
                        DownloadTools.DownloadBytes(r.result.Meta.ThumbnailURL, b =>
                        {
                            if (!IsInstanceValid(background))
                                return;
                            bool img = ImageTools.LoadImage(background, b);
                            if (!img)
                            {
                                ImageTools.LoadFFmpeg(videoBackground, b);
                                background.Hide();
                            }
                        });
                        isLoaded = true;
                        shouldShow = true;
                    });
                }
                else
                    isLoaded = true;
            }, instance.WorldId);
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
                            bool img = ImageTools.LoadImage(background, b);
                            if (!img)
                            {
                                ImageTools.LoadFFmpeg(videoBackground, b);
                                background.Hide();
                            }
                        });
                        icon.Hide();
                        isLoaded = true;
                        shouldShow = true;
                    });
                }
                else
                    isLoaded = true;
            }, worldId);
        }

        public void SetAvatarId(string avatarId)
        {
            Reset();
            APITools.APIObject.GetAvatarMeta(r =>
            {
                if (r.success)
                {
                    QuickInvoke.InvokeActionOnMainThread(() =>
                    {
                        if (!IsInstanceValid(label))
                            return;
                        type = CardType.Avatar;
                        cardInfoId = avatarId;
                        avatarMeta = r.result.Meta;
                        label.Text = r.result.Meta.Name.Replace("[", "[lb]");
                        DownloadTools.DownloadBytes(r.result.Meta.ImageURL, b =>
                        {
                            if (!IsInstanceValid(background))
                                return;
                            bool img = ImageTools.LoadImage(background, b);
                            if (!img)
                            {
                                ImageTools.LoadFFmpeg(videoBackground, b);
                                background.Hide();
                            }
                        });
                        icon.Hide();
                        isLoaded = true;
                        shouldShow = true;
                    });
                }
                else
                    isLoaded = true;
            }, avatarId);
        }

        public void SetUserId(string userId, CardUserType utype)
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
                        userType = utype;
                        userData = r.result.UserData;
                        label.Text = $"{r.result.UserData.GetUsersName().Replace("[", "[lb]")} [color={GetColor(r.result.UserData.Bio.Status)}]{r.result.UserData.Bio.Status}[/color]";
                        DownloadTools.DownloadBytes(r.result.UserData.Bio.PfpURL, b =>
                        {
                            if (!IsInstanceValid(icon))
                                return;
                            bool img = ImageTools.LoadImage(icon, b);
                            if (!img)
                            {
                                ImageTools.LoadFFmpeg(videoIcon, b);
                                icon.Hide();
                            }
                        });
                        DownloadTools.DownloadBytes(r.result.UserData.Bio.BannerURL, b =>
                        {
                            if (!IsInstanceValid(background))
                                return;
                            bool img = ImageTools.LoadImage(background, b);
                            if (!img)
                            {
                                ImageTools.LoadFFmpeg(videoBackground, b);
                                background.Hide();
                            }
                        });
                        isLoaded = true;
                        shouldShow = true;
                    });
                }
                else
                    isLoaded = true;
            }, userId, isUserId: true);
        }
    }
}
