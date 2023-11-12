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
        public Vector3 Pos { get => GetPart<PlayerController>().Position; set => GetPart<PlayerController>().Position = value; }
        private Vector3 oldPosition;

        public T GetPart<T>() where T : Node
        {
            return (T)Parts.FirstOrDefault(x => x is T);
        }

        public override void _Ready()
        {
            Position = Vector3.Zero;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (Local != this)
                return;
            float tolerance = 1f;
            if (Mathf.IsEqualApprox(Pos.X, oldPosition.X, tolerance) && Mathf.IsEqualApprox(Pos.Y, oldPosition.Y, tolerance) && Mathf.IsEqualApprox(Pos.Z, oldPosition.Z, tolerance))
                return;
            GameInstance.FocusedInstance.SendMessage(new PlayerObjectUpdate()
            {
                Auth = new JoinAuth()
                {
                    TempToken = GameInstance.FocusedInstance.userIdToken,
                    UserId = APITools.CurrentUser.Id,
                },
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

        public void SetUser(string userid, GameInstance instance)
        {
            UserId = userid;
            if (UserId == APITools.CurrentUser.Id)
            {
                Local = this;
            }
        }

        public void ResetWeights()
        {
        }
    }
}
