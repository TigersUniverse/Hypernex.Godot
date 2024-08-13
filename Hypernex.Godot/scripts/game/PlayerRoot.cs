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
        private Vector3 oldPosition;
        public bool IsLocal => Local == this;
        public string AvatarId;
        public AvatarRoot Avatar;
        public Action OnUserSet = () => { };

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
                    });
                }
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!IsLocal)
                return;
            float tolerance = 0.05f;
            if (Mathf.IsEqualApprox(Pos.X, oldPosition.X, tolerance) && Mathf.IsEqualApprox(Pos.Y, oldPosition.Y, tolerance) && Mathf.IsEqualApprox(Pos.Z, oldPosition.Z, tolerance))
                return;
            Instance.SendMessage(new PlayerObjectUpdate()
            {
                Auth = GetJoinAuth(),
                Objects = GetObjects(),
            }, Nexport.MessageChannel.Unreliable);
            oldPosition = Pos;
        }

        public Dictionary<int, NetworkedObject> GetObjects()
        {
            Dictionary<int, NetworkedObject> dict = new Dictionary<int, NetworkedObject>();
            dict.Add(0, new Networking.Messages.Data.NetworkedObject()
            {
                ObjectLocation = "root",
                IgnoreObjectLocation = true,
                Position = Pos.ToFloat3(),
                Rotation = Rot.ToFloat4(),
                Size = Vector3.One.ToFloat3(),
            });
            return dict;
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
            if (playerObjectUpdate.Objects.TryGetValue(0, out var val))
            {
                Pos = val.Position.ToGodot3();
                Rot = val.Rotation.ToGodotQuat();
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
