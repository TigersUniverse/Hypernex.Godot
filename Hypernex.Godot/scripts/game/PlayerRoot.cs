using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using HypernexSharp.APIObjects;

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
        public Label3D username;
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
        public AvatarRoot Avatar;
        public Action OnUserSet = () => { };

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
            }
            APITools.APIObject.GetUser(r =>
            {
                if (r.success)
                {
                    User = r.result.UserData;
                    username.Text = User.GetUsersName();
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
            // Position = Vector3.Zero;
            UpdateLoop();
            if (IsInstanceValid(Avatar))
                Avatar.QueueFree();
            targetPos.Clear();
            targetRot.Clear();
            Avatar = AvatarRoot.LoadFromFile("user://skeleton.hna");
            AddChild(Avatar);
            Avatar.AttachTo(Controller);
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
                        // PlayerAssignedTags = new List<string>(),
                        // ExtraneousData = new Dictionary<string, object>(),
                        // WeightedObjects = new Dictionary<string, float>(),
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
                    Avatar.ProcessIk(false, true, view.GlobalTransform, Transform3D.Identity, Transform3D.Identity);
                    Avatar.ikSystem.head.Scale = Vector3.One * 0.01f;
                }
                else if (!IsLocal)
                    Avatar.ProcessIk(false, false, Transform3D.Identity, Transform3D.Identity, Transform3D.Identity);
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
            if (IsInstanceValid(Avatar))
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
            return dict;
        }

        public void LerpTarget(int id, Node3D node, float speed)
        {
            if (targetPos.ContainsKey(id) && targetRot.ContainsKey(id))
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

        public void NetworkUpdate(PlayerUpdate playerUpdate)
        {
            if (playerUpdate.AvatarId != AvatarId)
            {
                AvatarId = playerUpdate.AvatarId;
                APITools.APIObject.GetAvatarMeta(result =>
                {
                    AvatarMeta avatarMeta = result.result.Meta;
                    string fileId = avatarMeta.Builds.First(x => x.BuildPlatform == BuildPlatform.Windows).FileId;
                    string fileURL = $"{APITools.APIObject.Settings.APIURL}file/{avatarMeta.OwnerId}/{fileId}";
                    if (result.success)
                    {
                        string knownHash = string.Empty;
                        APITools.APIObject.GetFileMeta(fileMetaResult =>
                        {
                            if (fileMetaResult.success)
                                knownHash = fileMetaResult.result.FileMeta.Hash;
                            DownloadTools.DownloadFile(fileURL, $"{avatarMeta.Id}.hna", o =>
                            {
                                if (!string.IsNullOrEmpty(o))
                                {
                                    if (IsInstanceValid(Avatar))
                                        Avatar.QueueFree();
                                    targetPos.Clear();
                                    targetRot.Clear();
                                    Avatar = AvatarRoot.LoadFromFile(o);
                                    AddChild(Avatar);
                                    Avatar.AttachTo(Controller);
                                }
                            }, knownHash, p => Init.Instance.loadingOverlay.Report(fileURL, p));
                        }, avatarMeta.OwnerId, fileId);
                    }
                }, playerUpdate.AvatarId);
            }
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
