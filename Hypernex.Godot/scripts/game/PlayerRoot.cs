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
        private Vector3 oldPosition;

        public T GetPart<T>() where T : Node
        {
            return (T)Parts.FirstOrDefault(x => x is T);
        }

        public override void _PhysicsProcess(double delta)
        {
            float tolerance = 0.1f;
            if (Mathf.IsEqualApprox(Position.X, oldPosition.X, tolerance) && Mathf.IsEqualApprox(Position.Y, oldPosition.Y, tolerance) && Mathf.IsEqualApprox(Position.Z, oldPosition.Z, tolerance))
                return;
            GameInstance.FocusedInstance.SendMessage(new PlayerObjectUpdate()
            {
                Object = new Networking.Messages.Data.NetworkedObject()
                {
                    ObjectLocation = "root",
                    Position = Position.ToFloat3(),
                }
            });
            oldPosition = Position;
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
                Position = playerObjectUpdate.Object.Position.ToGodot3();
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
