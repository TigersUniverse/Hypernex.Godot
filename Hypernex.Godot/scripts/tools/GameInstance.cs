using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Hypernex.CCK;
using Hypernex.Game;
using Hypernex.Networking;
using Hypernex.Networking.Messages;
using Hypernex.Player;
using Hypernex.Sandboxing;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketResponses;
using HypernexSharp.SocketObjects;
using Nexport;

namespace Hypernex.Tools
{
    public class GameInstance
    {
        public static GameInstance FocusedInstance { get; internal set; }
        public static Action<GameInstance, WorldMeta> OnGameInstanceLoaded { get; set; } = (instance, meta) => { };

        internal static void Initialize()
        {
            // main thread
            SocketManager.OnInstanceJoined += (instance, meta) =>
            {
                if (FocusedInstance != null && FocusedInstance.gameServerId == instance.gameServerId &&
                    FocusedInstance.instanceId == instance.instanceId)
                {
                    // Reconnect Socket
                    Logger.CurrentLogger.Debug("Instance reconnected.");
                    return;
                }
                GameInstance gameInstance = new GameInstance(instance, meta);
                gameInstance.Load();
            };
            SocketManager.OnInstanceLeft += instance =>
            {
                if (FocusedInstance != null && FocusedInstance.gameServerId == instance.gameServerId &&
                    FocusedInstance.instanceId == instance.instanceId)
                {
                    FocusedInstance.Dispose();
                }
            };
            SocketManager.OnInstanceOpened += (opened, meta) =>
            {
                GameInstance gameInstance = new GameInstance(opened, meta);
                gameInstance.Load();
            };
        }

        public Action OnConnect { get; set; } = () => { };
        public Action<User> OnUserLoaded { get; set; } = user => { };
        public Action<User> OnClientConnect { get; set; } = user => { };
        public Action<MsgMeta, MessageChannel> OnMessage { get; set; } = (meta, channel) => { };
        public Action<User> OnClientDisconnect { get; set; } = identifier => { };
        public Action OnDisconnect { get; set; } = () => { };

        public bool IsOpen => client?.IsOpen ?? false;
        public List<User> ConnectedUsers => client.ConnectedUsers;

        public bool CanInvite
        {
            get
            {
                if (instanceCreatorId == APITools.CurrentUser.Id)
                    return true;
                switch (Publicity)
                {
                    case InstancePublicity.Anyone:
                        return true;
                    case InstancePublicity.Acquaintances:
                    case InstancePublicity.Friends:
                    case InstancePublicity.OpenRequest:
                        if (host == null)
                            return false;
                        return host.Friends.Contains(APITools.CurrentUser.Id);
                    case InstancePublicity.ModeratorRequest:
                        return Moderators.Contains(APITools.CurrentUser.Id);
                }
                return false;
            }
        }

        public bool IsModerator => Moderators.Contains(APITools.CurrentUser.Id);

        public List<string> Moderators = new();
        public List<string> BannedUsers = new();
        public List<string> SocketConnectedUsers = new();

        public string gameServerId;
        public string instanceId;
        public string userIdToken;
        public WorldMeta worldMeta;
        public WorldRoot World;
        public User host;
        public Texture2D Thumbnail;
        public InstancePublicity Publicity;
        public bool lockAvatarSwitching;

        private HypernexInstanceClient client;
        internal bool authed;
        internal List<ScriptRunner> sandboxes => World?.Runners;
        public readonly string instanceCreatorId;

        private string hostId => client?.HostId ?? string.Empty;
        internal bool isHost
        {
            get
            {
                if (APITools.CurrentUser == null)
                    return false;
                if (client == null)
                    return false;
                return client.HostId == APITools.CurrentUser.Id;
            }
        }
        private List<User> usersBeforeMe = new ();
        private bool isDisposed;
        public bool IsDisposed => isDisposed;
        // internal ScriptEvents ScriptEvents;

        private GameInstance(JoinedInstance joinInstance, WorldMeta worldMeta)
        {
            FocusedInstance?.Dispose();
            gameServerId = joinInstance.gameServerId;
            instanceId = joinInstance.instanceId;
            userIdToken = joinInstance.tempUserToken;
            this.worldMeta = worldMeta;
            instanceCreatorId = joinInstance.instanceCreatorId;
            Publicity = joinInstance.InstancePublicity;
            Moderators = joinInstance.Moderators;
            BannedUsers = joinInstance.BannedUsers;
            string[] s = joinInstance.Uri.Split(':');
            string ip = s[0];
            int port = Convert.ToInt32(s[1]);
            InstanceProtocol instanceProtocol = joinInstance.InstanceProtocol;
            SetupClient(ip, port, instanceProtocol);
        }

        private GameInstance(InstanceOpened instanceOpened, WorldMeta worldMeta)
        {
            FocusedInstance?.Dispose();
            gameServerId = instanceOpened.gameServerId;
            instanceId = instanceOpened.instanceId;
            userIdToken = instanceOpened.tempUserToken;
            this.worldMeta = worldMeta;
            instanceCreatorId = APITools.CurrentUser.Id;
            Publicity = instanceOpened.InstancePublicity;
            Moderators = instanceOpened.Moderators;
            BannedUsers = instanceOpened.BannedUsers;
            string[] s = instanceOpened.Uri.Split(':');
            string ip = s[0];
            int port = Convert.ToInt32(s[1]);
            InstanceProtocol instanceProtocol = instanceOpened.InstanceProtocol;
            SetupClient(ip, port, instanceProtocol);
        }

        private void SetupClient(string ip, int port, InstanceProtocol instanceProtocol)
        {
            // ScriptEvents = new ScriptEvents(this);
            ClientSettings clientSettings = new ClientSettings(ip, port, instanceProtocol == InstanceProtocol.UDP, 1);
            client = new HypernexInstanceClient(APITools.APIObject, APITools.CurrentUser, instanceProtocol,
                clientSettings);
            client.OnConnect += () =>
            {
                QuickInvoke.InvokeActionOnMainThread(OnConnect);
            };
            client.OnUserLoaded += user => QuickInvoke.InvokeActionOnMainThread(OnUserLoaded, user);
            client.OnClientConnect += user => QuickInvoke.InvokeActionOnMainThread(OnClientConnect, user);
            client.OnMessage += (message, meta) => QuickInvoke.InvokeActionOnMainThread(OnMessage, message, meta);
            client.OnClientDisconnect += user => QuickInvoke.InvokeActionOnMainThread(OnClientDisconnect, user);
            client.OnDisconnect += () =>
            {
                if (isDisposed)
                    return;
                // Verify they actually leave the socket instance too
                SocketManager.LeaveInstance(gameServerId, instanceId);
                QuickInvoke.InvokeActionOnMainThread(OnDisconnect);
            };
            // OnClientConnect += user => ScriptEvents?.OnUserJoin.Invoke(user.Id);
            OnMessage += (meta, channel) => MessageHandler.HandleMessage(this, meta, channel);
            OnClientDisconnect += user => PlayerManagement.PlayerLeave(this, user);
            OnDisconnect += Dispose;
            PlayerManagement.CreateGameInstance(this);
            APITools.UserSocket.OnOpen += () =>
            {
                // Socket probably reconnected, rejoin instance
                SocketManager.JoinInstance(new SafeInstance
                {
                    GameServerId = gameServerId,
                    InstanceId = instanceId
                });
            };
            APITools.OnLogout += Dispose;
        }

        public void Open()
        {
            if (!client.IsOpen)
                client.Open();
        }

        public void Close()
        {
            DiscordTools.UnfocusInstance(gameServerId + "/" + instanceId);
            PlayerManagement.DestroyGameInstance(this);
            if (IsOpen)
                client?.Stop();
        }

        public void SendMessage<T>(T message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            SendMessage(Msg.Serialize(message), messageChannel);
        }

        /// <summary>
        /// Sends a message over the client. If this is multithreaded, DO NOT PASS GODOT OBJECTS.
        /// </summary>
        /// <param name="message">message to send</param>
        /// <param name="messageChannel">channel to send message over</param>
        public void SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            if (authed && IsOpen)
                client.SendMessage(message, messageChannel);
        }

        public void InviteUser(User user)
        {
            if (!CanInvite)
                return;
            SocketManager.InviteUser(this, user);
        }

        public void WarnUser(User user, string message)
        {
            if (!IsModerator)
                return;
            WarnPlayer warnPlayer = new WarnPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APITools.CurrentUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id,
                message = message
            };
            SendMessage(Msg.Serialize(warnPlayer));
        }

        public void KickUser(User user, string message)
        {
            if (!IsModerator)
                return;
            KickPlayer kickPlayer = new KickPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APITools.CurrentUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id,
                message = message
            };
            SendMessage(Msg.Serialize(kickPlayer));
        }

        public void BanUser(User user, string message)
        {
            if (!IsModerator)
                return;
            BanPlayer banPlayer = new BanPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APITools.CurrentUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id,
                message = message
            };
            SendMessage(Msg.Serialize(banPlayer));
        }

        public void UnbanUser(User user)
        {
            if (!IsModerator)
                return;
            UnbanPlayer unbanPlayer = new UnbanPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APITools.CurrentUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id
            };
            SendMessage(Msg.Serialize(unbanPlayer));
        }

        public void AddModerator(User user)
        {
            if (!IsModerator)
                return;
            AddModerator addModerator = new AddModerator
            {
                Auth = new JoinAuth
                {
                    UserId = APITools.CurrentUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id
            };
            SendMessage(Msg.Serialize(addModerator));
        }

        public void RemoveModerator(User user)
        {
            if (!IsModerator)
                return;
            RemoveModerator removeModerator = new RemoveModerator
            {
                Auth = new JoinAuth
                {
                    UserId = APITools.CurrentUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id
            };
            SendMessage(Msg.Serialize(removeModerator));
        }

        internal void __SendMessage<T>(T message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            __SendMessage(Msg.Serialize(message), messageChannel);
        }

        internal void __SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            if (IsOpen)
                client.SendMessage(message, messageChannel);
        }

        internal void UpdateInstanceMeta(UpdatedInstance updatedInstance)
        {
            Moderators = new List<string>(updatedInstance.instanceMeta.Moderators);
            BannedUsers = new List<string>(updatedInstance.instanceMeta.BannedUsers);
            SocketConnectedUsers = new List<string>(updatedInstance.instanceMeta.ConnectedUsers);
        }

        private void LoadScene(bool open, string s)
        {
            try
            {
                World = WorldRoot.LoadFromFile(s);
                if (World == null)
                {
                    Dispose();
                    return;
                }
                if (open)
                    Open();
                FocusedInstance = this;
                OnGameInstanceLoaded?.Invoke(this, worldMeta);
            }
            catch (Exception)
            {
                Dispose();
            }
        }

        private void Load(bool open = true)
        {
            if (SocketManager.DownloadedWorlds.ContainsKey(worldMeta.Id) &&
                File.Exists(SocketManager.DownloadedWorlds[worldMeta.Id]))
            {
                string o = SocketManager.DownloadedWorlds[worldMeta.Id];
                if (!string.IsNullOrEmpty(o))
                    LoadScene(open, o);
                else
                    Dispose();
            }
            else
            {
                // Download World
                string fileId;
                try
                {
                    fileId = worldMeta.Builds.First(x => x.BuildPlatform == BuildPlatform.Windows).FileId;
                }
                catch (InvalidOperationException)
                {
                    Dispose();
                    return;
                }
                string fileURL = $"{APITools.APIObject.Settings.APIURL}file/{worldMeta.OwnerId}/{fileId}";
                APITools.APIObject.GetFileMeta(fileMetaResult =>
                {
                    string knownHash = string.Empty;
                    if (fileMetaResult.success)
                        knownHash = fileMetaResult.result.FileMeta.Hash;
                    DownloadTools.DownloadFile(fileURL, $"{worldMeta.Id}.hnw", o =>
                    {
                        if (!string.IsNullOrEmpty(o))
                            LoadScene(open, o);
                        else
                            Dispose();
                    }, knownHash);
                }, worldMeta.OwnerId, fileId);
            }
        }

        internal void FixedUpdate()
        {
            sandboxes?.ForEach(x => x.runtime.FixedUpdate());
        }

        internal void Update()
        {
            if (!authed && IsOpen)
            {
                /*
                __SendMessage(new JoinAuth
                {
                    UserId = APITools.CurrentUser.Id,
                    TempToken = userIdToken,
                });
                */
                return;
            }
            if (!string.IsNullOrEmpty(hostId) && (host == null || (host != null && host.Id != hostId)))
            {
                if (hostId == APITools.CurrentUser.Id)
                    host = APITools.CurrentUser;
                else
                    foreach (User connectedUser in ConnectedUsers)
                    {
                        if (connectedUser.Id == hostId)
                        {
                            host = connectedUser;
                        }
                    }
                if (host != null)
                    DiscordTools.FocusInstance(worldMeta, gameServerId + "/" + instanceId, host);
            }
            sandboxes?.ForEach(x => x.runtime.Update());
        }

        internal void LateUpdate()
        {
            sandboxes?.ForEach(x => x.runtime.LateUpdate());
        }

        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            FocusedInstance = null;
            OnGameInstanceLoaded?.Invoke(this, worldMeta);
            World?.QueueFree();
            // Physics.gravity = new Vector3(0, LocalPlayer.Instance.Gravity, 0);
            sandboxes?.ForEach(x => x.Stop());
            Close();
        }
    }
}
