using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Networking.Messages;
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
        public NodePath[] parts = Array.Empty<NodePath>();
        public Node[] Parts => parts.Select(x => GetNode(x)).ToArray();
        public string UserId { get; private set; }
        public User User { get; private set; }
        public GameInstance Instance { get; private set; }
        public PlayerController Controller => GetPart<PlayerController>();
        public Vector3 Pos { get => Controller.Position; set => Controller.Position = value; }
        private Vector3 oldPosition;
        public bool IsLocal => Local == this;
        public string AvatarId;
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
                    User = r.result.UserData;
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
            Position = Vector3.Zero;
            UpdateLoop();
        }

        private async void UpdateLoop()
        {
            while (IsInstanceValid(this))
            {
                if (Instance.IsOpen)
                {
                    Instance.SendMessage(new PlayerUpdate()
                    {
                        Auth = GetJoinAuth(),
                        AvatarId = AvatarId,
                        IsSpeaking = GetPart<PlayerChat>()?.IsSpeaking ?? false,
                        IsPlayerVR = false, // TODO: vr
                        PlayerAssignedTags = new List<string>(),
                        ExtraneousData = new Dictionary<string, object>(),
                        WeightedObjects = new Dictionary<string, float>(),
                    });
                }
                await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
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
                Object = new Networking.Messages.Data.NetworkedObject()
                {
                    ObjectLocation = "root",
                    Position = Pos.ToFloat3(),
                },
            }, Nexport.MessageChannel.Unreliable);
            oldPosition = Pos;
        }

        public void NetworkUpdate(PlayerUpdate playerUpdate)
        {
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
            if (playerObjectUpdate.Object.ObjectLocation.ToLower() == "root")
            {
                Pos = playerObjectUpdate.Object.Position.ToGodot3();
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
