using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Networking.Messages;
using Hypernex.Player;
using Hypernex.Tools;
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

        public T GetPart<T>() where T : Node
        {
            return (T)Parts.FirstOrDefault(x => x is T);
        }

        public void NetworkUpdate(PlayerUpdate playerUpdate)
        {
        }

        public void VoiceUpdate(PlayerVoice playerVoice)
        {
        }

        public void NetworkObjectUpdate(PlayerObjectUpdate playerObjectUpdate)
        {
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
