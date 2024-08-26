using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;
using Hypernex.CCK;
using Hypernex.Configuration;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketMessages;

namespace Hypernex.Game
{
    public partial class PlayerRoot : Node3D
    {
        public const int RootId = 0;
        public const int HeadId = 1;
        public const int LeftHandId = 2;
        public const int RightHandId = 3;

        public static PlayerRoot Local { get; private set; }
        [Export]
        public Node3D view;
        [Export]
        public ProgressBar loadingBar;
        [Export]
        public Sprite3D loadingSprite;
        [Export]
        public Label username;
        [Export]
        public NodePath[] parts = Array.Empty<NodePath>();
        public Node[] Parts => parts.Select(x => GetNode(x)).ToArray();
        public string UserId { get; private set; }
        public User User { get; private set; }
        public GameInstance Instance { get; private set; }
        public Node3D Controller => /*GetViewport().UseXR ? GetPart<XRPlayerController>().xrBody :*/ GetPart<PlayerController>();
        public Vector3 Pos { get => Controller.Position; set => Controller.Position = value; }
        public Quaternion Rot { get => Controller.Quaternion; set => Controller.Quaternion = value; }
        public bool IsLocal => Local == this;
        public string AvatarId;
        public string TargetAvatarId;
        public bool waitingForAvatarToken = false;
        public AvatarRoot Avatar;
        public Action OnUserSet = () => { };
        private Dictionary<string, string> assetTokens = new Dictionary<string, string>();

        public Dictionary<int, Vector3> targetPos = new Dictionary<int, Vector3>();
        public Dictionary<int, Quaternion> targetRot = new Dictionary<int, Quaternion>();

        public T GetPart<T>() where T : Node
        {
            return (T)Parts.FirstOrDefault(x => x is T);
        }

        public void SetUser(string userid, GameInstance instance)
        {
            UserId = userid;
            Instance = instance;
            if (UserId == APITools.CurrentUser.Id)
            {
                Local = this;
                Instance.OnClientConnect += OtherUserLoaded;
                ChangeAvatar(ConfigManager.SelectedConfigUser.CurrentAvatar);
            }
            else
                SocketManager.OnAvatarToken += OnAssetToken;
            APITools.APIObject.GetUser(r =>
            {
                if (r.success)
                {
                    QuickInvoke.InvokeActionOnMainThread(() =>
                    {
                        User = r.result.UserData;
                        if (IsInstanceValid(username))
                            username.Text = User.GetUsersName();
                    });
                }
            }, UserId, isUserId: true);
            GetPart<PlayerChat>()?.UserSet();
            OnUserSet?.Invoke();
        }

        public JoinAuth GetJoinAuth()
        {
            return new JoinAuth()
            {
                TempToken = Instance.userIdToken,
                UserId = UserId,
            };
        }

        public override void _Ready()
        {
            UpdateLoop();
        }

        public override void _ExitTree()
        {
            if (IsLocal)
            {
                foreach (var token in assetTokens)
                    APITools.APIObject.RemoveAssetToken(_ => { }, APITools.CurrentUser, APITools.CurrentToken, token.Key, token.Value);
            }
        }

        private async void UpdateLoop()
        {
            while (IsInstanceValid(this))
            {
                await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
                if (Instance.IsOpen)
                {
                    Instance.SendMessage(new PlayerUpdate()
                    {
                        Auth = GetJoinAuth(),
                        AvatarId = AvatarId,
                        IsSpeaking = GetPart<PlayerChat>()?.IsSpeaking ?? false,
                        IsPlayerVR = GetViewport().UseXR,
                        IsFBT = false,
                        VRIKJson = "",
                    }, Nexport.MessageChannel.Unreliable);
                    Instance.SendMessage(new PlayerObjectUpdate()
                    {
                        Auth = GetJoinAuth(),
                        Objects = GetObjects(),
                    }, Nexport.MessageChannel.Unreliable);
                }
            }
        }

        public override void _Process(double delta)
        {
            if (IsInstanceValid(Avatar))
            {
                if (IsLocal && !Init.IsVRLoaded)
                {
                    view.Position = new Vector3(0f, Avatar.ikSystem.floorDistance + Avatar.ikSystem.hipsDistance * 0.75f, 0f);
                    Avatar.ProcessIk(false, true, view.GlobalTransform.Translated(view.GlobalBasis.Z * 0.2f), Controller.GlobalTransform/*.Translated(Controller.GlobalBasis.Y * 0.05f)*/, Transform3D.Identity, Transform3D.Identity);
                    // if (IsInstanceValid(Avatar.ikSystem))
                        // Avatar.ikSystem.head.Scale = Vector3.One * 0.01f;
                }
                else if (!IsLocal)
                    Avatar.ProcessIk(false, false, Transform3D.Identity, Transform3D.Identity, Transform3D.Identity, Transform3D.Identity);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!IsLocal)
            {
                float lerpSpeed = 10f;
                LerpTarget(RootId, Controller, (float)delta * lerpSpeed);
                if (IsInstanceValid(Avatar))
                {
                    LerpTarget(HeadId, Avatar.HeadTransform, (float)delta * lerpSpeed);
                    LerpTarget(LeftHandId, Avatar.LeftHandTransform, (float)delta * lerpSpeed);
                    LerpTarget(RightHandId, Avatar.RightHandTransform, (float)delta * lerpSpeed);
                }
                return;
            }
        }

        public Dictionary<int, NetworkedObject> GetObjects()
        {
            Dictionary<int, NetworkedObject> dict = new Dictionary<int, NetworkedObject>();
            dict.Add(RootId, new NetworkedObject()
            {
                ObjectLocation = "root",
                IgnoreObjectLocation = true,
                Position = Pos.ToFloat3(),
                Rotation = Rot.ToFloat4(),
                Size = Vector3.One.ToFloat3(),
            });
            try
            {
                if (IsInstanceValid(Avatar) && IsInstanceValid(Avatar.ikSystem))
                {
                    dict.Add(HeadId, new NetworkedObject()
                    {
                        ObjectLocation = "head",
                        IgnoreObjectLocation = true,
                        Position = Avatar.HeadTransform.Position.ToFloat3(),
                        Rotation = Avatar.HeadTransform.Basis.GetRotationQuaternion().Normalized().ToFloat4(),
                        Size = Vector3.One.ToFloat3(),
                    });
                    dict.Add(LeftHandId, new NetworkedObject()
                    {
                        ObjectLocation = "left_hand",
                        IgnoreObjectLocation = true,
                        Position = Avatar.LeftHandTransform.Position.ToFloat3(),
                        Rotation = Avatar.LeftHandTransform.Basis.GetRotationQuaternion().Normalized().ToFloat4(),
                        Size = Vector3.One.ToFloat3(),
                    });
                    dict.Add(RightHandId, new NetworkedObject()
                    {
                        ObjectLocation = "right_hand",
                        IgnoreObjectLocation = true,
                        Position = Avatar.RightHandTransform.Position.ToFloat3(),
                        Rotation = Avatar.RightHandTransform.Basis.GetRotationQuaternion().Normalized().ToFloat4(),
                        Size = Vector3.One.ToFloat3(),
                    });
                }
            }
            catch
            { }
            return dict;
        }

        public void LerpTarget(int id, Node3D node, float speed)
        {
            if (targetPos.ContainsKey(id) && targetRot.ContainsKey(id) && IsInstanceValid(node))
            {
                Vector3 scl = node.Scale;
                node.Position = node.Position.Lerp(targetPos[id], speed);
                node.Basis = node.Basis.Slerp(new Basis(targetRot[id]), speed);
                node.Scale = scl;
            }
        }

        public void SetTarget(int id, Vector3 position, Quaternion rotation)
        {
            if (!targetPos.ContainsKey(id))
                targetPos.Add(id, Vector3.Zero);
            if (!targetRot.ContainsKey(id))
                targetRot.Add(id, Quaternion.Identity);
            targetPos[id] = position;
            targetRot[id] = rotation;
        }

        public void LoadAvatar(string path, string id)
        {
            AvatarId = id;
            if (IsInstanceValid(loadingSprite))
                loadingSprite.Visible = false;
            targetPos.Clear();
            targetRot.Clear();
            if (string.IsNullOrEmpty(path))
            {
                if (IsInstanceValid(Avatar))
                    Avatar.QueueFree();
                Avatar = null;
                return;
            }
            new Thread(() =>
            {
                var avi = AvatarRoot.LoadFromFile(path);
                QuickInvoke.InvokeActionOnMainThread(() =>
                {
                    if (IsInstanceValid(Avatar))
                        Avatar.QueueFree();
                    Avatar = avi;
                    if (IsInstanceValid(Avatar))
                    {
                        AddChild(Avatar);
                        Avatar.AttachTo(Controller);
                    }
                });
            }).Start();
        }

        private void OnAssetToken(SharedAvatarToken shToken)
        {
            if (shToken.avatarId == TargetAvatarId && waitingForAvatarToken && shToken.fromUserId == UserId)
            {
                waitingForAvatarToken = false;
                ChangeAvatar(shToken.avatarId);
            }
        }

        private void OtherUserLoaded(User user)
        {
            if (!string.IsNullOrEmpty(AvatarId))
                ShareAvatarToken(user, AvatarId);
            if (!string.IsNullOrEmpty(TargetAvatarId))
                ShareAvatarToken(user, TargetAvatarId);
        }

        public void NetworkUpdate(PlayerUpdate playerUpdate)
        {
            if (playerUpdate.AvatarId != AvatarId && playerUpdate.AvatarId != TargetAvatarId)
            {
                ChangeAvatar(playerUpdate.AvatarId);
            }
        }

        private void ReportDownloadProgress(string id, float amount)
        {
            if (IsInstanceValid(loadingBar) && IsInstanceValid(loadingSprite) && TargetAvatarId == id)
            {
                loadingBar.Value = amount;
                loadingSprite.Visible = amount < 1f;
            }
        }

        private void ShareAvatarToken(User user, string id)
        {
            if (APITools.CurrentUser.BlockedUsers.Contains(user.Id))
                return;
            GetAvatarToken(id, token =>
            {
                APITools.UserSocket.ShareAvatarToken(user, id, token);
            });
        }

        private void GetAvatarToken(string id, Action<string> callback)
        {
            APITools.APIObject.AddAssetToken(result =>
            {
                if (result.success)
                {
                    if (IsInstanceValid(this))
                        QuickInvoke.InvokeActionOnMainThread(callback, result.result.token.content);
                }
                else
                {
                    QuickInvoke.InvokeActionOnMainThread(callback, (string)null);
                }
            }, APITools.CurrentUser, APITools.CurrentToken, id);
        }

        private void DownloadAvatar(AvatarMeta avatarMeta, string token)
        {
            string fileId = avatarMeta.Builds.First(x => x.BuildPlatform == BuildPlatform.Windows).FileId;
            string fileURL = $"{APITools.APIObject.Settings.APIURL}file/{avatarMeta.OwnerId}/{fileId}";
            if (!string.IsNullOrEmpty(token))
                fileURL += $"/{token}";
            string knownHash = string.Empty;
            APITools.APIObject.GetFileMeta(fileMetaResult =>
            {
                if (fileMetaResult.success)
                {
                    knownHash = fileMetaResult.result.FileMeta.Hash;
                    DownloadTools.DownloadFile(fileURL, $"{avatarMeta.Id}.hna", o =>
                    {
                        LoadAvatar(o, avatarMeta.Id);
                    }, knownHash, p => ReportDownloadProgress(avatarMeta.Id, p));
                }
                /*
                else if (string.IsNullOrEmpty(token))
                {
                    if (avatarMeta.Publicity == AvatarPublicity.Anyone)
                    {
                        APITools.APIObject.GetFile(stream =>
                        {
                            LoadAvatar(DownloadTools.CopyToFile($"{avatarMeta.Id}.hna", stream));
                        }, avatarMeta.OwnerId, fileId);
                    }
                    else
                    {
                        LoadAvatar(string.Empty);
                    }
                }
                else
                {
                    APITools.APIObject.GetFile(stream =>
                    {
                        LoadAvatar(DownloadTools.CopyToFile($"{avatarMeta.Id}.hna", stream));
                    }, avatarMeta.OwnerId, fileId, token);
                }
                */
            }, avatarMeta.OwnerId, fileId);
        }

        public void ChangeAvatar(string id)
        {
            if (AvatarId == id && IsInstanceValid(Avatar))
                return;
            TargetAvatarId = id;
            if (string.IsNullOrEmpty(id))
                return;
            APITools.APIObject.GetAvatarMeta(result =>
            {
                AvatarMeta avatarMeta = result.result.Meta;
                if (avatarMeta.Publicity != AvatarPublicity.Anyone)
                {
                    if (avatarMeta.OwnerId != APITools.CurrentUser.Id)
                    {
                        SharedAvatarToken shToken = SocketManager.SharedAvatarTokens.FirstOrDefault(x => x.avatarId == avatarMeta.Id && x.fromUserId == UserId);
                        if (shToken != null)
                        {
                            DownloadAvatar(avatarMeta, shToken.avatarToken);
                        }
                        else
                        {
                            QuickInvoke.InvokeActionOnMainThread(() =>
                            {
                                waitingForAvatarToken = true;
                                AvatarId = string.Empty;
                                if (IsInstanceValid(Avatar))
                                    Avatar.QueueFree();
                                Avatar = null;
                            });
                        }
                    }
                    else
                    {
                        GetAvatarToken(avatarMeta.Id, token =>
                        {
                            assetTokens.TryAdd(avatarMeta.Id, token);
                            DownloadAvatar(avatarMeta, token);
                        });
                        foreach (var user in Instance.ConnectedUsers)
                        {
                            ShareAvatarToken(user, avatarMeta.Id);
                        }
                    }
                }
                else
                {
                    DownloadAvatar(avatarMeta, null);
                }
            }, id);
        }

        public void VoiceUpdate(PlayerVoice playerVoice)
        {
            GetPart<PlayerChat>()?.HandleVoice(playerVoice);
        }

        public void MessageUpdate(PlayerMessage playerMessage)
        {
            GetPart<PlayerChat>()?.HandleMessage(playerMessage);
        }

        public void NetworkObjectUpdate(PlayerObjectUpdate playerObjectUpdate)
        {
            if (playerObjectUpdate.Objects.TryGetValue(RootId, out var body))
            {
                SetTarget(RootId, body.Position.ToGodot3(), body.Rotation.ToGodotQuat());
            }
            if (playerObjectUpdate.Objects.TryGetValue(HeadId, out var head))
            {
                SetTarget(HeadId, head.Position.ToGodot3(), head.Rotation.ToGodotQuat());
            }
            if (playerObjectUpdate.Objects.TryGetValue(LeftHandId, out var leftHand))
            {
                SetTarget(LeftHandId, leftHand.Position.ToGodot3(), leftHand.Rotation.ToGodotQuat());
            }
            if (playerObjectUpdate.Objects.TryGetValue(RightHandId, out var rightHand))
            {
                SetTarget(RightHandId, rightHand.Position.ToGodot3(), rightHand.Rotation.ToGodotQuat());
            }
        }

        public void ResetWeightedObjects()
        {
        }

        public void WeightedObject(WeightedObjectUpdate weightedObjectUpdate)
        {
        }

        public void ResetWeights()
        {
        }
    }
}
