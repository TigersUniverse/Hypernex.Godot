using Godot;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using Hypernex.Tools.Godot;

namespace Hypernex.Game
{
    public partial class NetworkSync : Node
    {
        public Node3D parent;
        public WorldRoot world;

        [Export]
        public bool InstanceHostOnly = false;
        [Export]
        public bool CanSteal = false;
        [Export]
        public bool AlwaysSync = false;

        public float lerpSpeed = 10f;
        public string NetworkOwner;
        public bool NetworkSteal = false;
        public bool isReleased = false;
        public Vector3 targetPosition;
        public Quaternion targetRotation;
        public Vector3 targetScale;
        public Vector3 vel;

        public bool IsOwned() => !string.IsNullOrEmpty(NetworkOwner);
        public bool IsOwnedByLocalPlayer() => APITools.CurrentUser == null || NetworkOwner == APITools.CurrentUser?.Id;

        public override void _Ready()
        {
            parent = GetParent<Node3D>();
            targetPosition = parent.Position;
            targetRotation = parent.Quaternion;
            targetScale = parent.Scale;
            if (InstanceHostOnly && world.gameInstance.isHost)
                Claim();
            UpdateLoop();
        }
        
        public void Claim()
        {
            if (IsOwned() || !NetworkSteal)
                return;
            isReleased = false;
            NetworkOwner = APITools.CurrentUser?.Id;
            world.gameInstance.SendMessage(GetObjectUpdate(), Nexport.MessageChannel.Reliable);
        }

        public void Unclaim()
        {
            if (!IsOwnedByLocalPlayer())
                return;
            if (AlwaysSync)
            {
                NetworkSteal = true;
                isReleased = true;
                world.gameInstance.SendMessage(GetObjectUpdate(), Nexport.MessageChannel.Reliable);
            }
            else
            {
                NetworkOwner = string.Empty;
                isReleased = false;
                WorldObjectUpdate update = GetObjectUpdate();
                update.Action = WorldObjectAction.Unclaim;
                world.gameInstance.SendMessage(update, Nexport.MessageChannel.Reliable);
            }
        }

        private WorldObjectAction GetAction()
        {
            if (IsOwnedByLocalPlayer())
                return WorldObjectAction.Update;
            NetworkOwner = APITools.CurrentUser?.Id;
            NetworkSteal = CanSteal;
            return WorldObjectAction.Claim;
        }

        private WorldObjectUpdate GetObjectUpdate()
        {
            return new WorldObjectUpdate()
            {
                Auth = GetJoinAuth(),
                Action = GetAction(),
                CanBeStolen = CanSteal || isReleased,
                Object = new NetworkedObject()
                {
                    ObjectLocation = world.GetLocalPath(this),
                    Position = parent.Position.ToFloat3(),
                    Rotation = parent.Quaternion.ToFloat4(),
                    Size = parent.Scale.ToFloat3(),
                    IgnoreObjectLocation = false,
                },
                // Velocity = vel,
            };
        }

        private async void UpdateLoop()
        {
            while (IsInstanceValid(this))
            {
                await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
                if (!IsInsideTree())
                    continue;
                if (world.gameInstance.IsOpen && IsOwnedByLocalPlayer())
                {
                    WorldObjectUpdate update = GetObjectUpdate();
                    world.gameInstance.SendMessage(update, update.Action == WorldObjectAction.Update ? Nexport.MessageChannel.Unreliable : Nexport.MessageChannel.Reliable);
                }
            }
        }

        private JoinAuth GetJoinAuth()
        {
            return new JoinAuth()
            {
                TempToken = world.gameInstance.userIdToken,
                UserId = APITools.CurrentUser.Id,
            };
        }

        public override void _Process(double delta)
        {
            if (!IsOwnedByLocalPlayer())
            {
                parent.Position = parent.Position.Lerp(targetPosition, (float)delta * lerpSpeed);
                parent.Quaternion = parent.Quaternion.Slerp(targetRotation, (float)delta * lerpSpeed);
                parent.Scale = parent.Scale.Lerp(targetScale, (float)delta * lerpSpeed);
            }
        }

        public void HandleMessage(WorldObjectUpdate update)
        {
            QuickInvoke.InvokeActionOnMainThread(() =>
            {
                if (IsInstanceValid(this))
                {
                    UpdateTransform(update);
                    NetworkOwner = update.Auth.UserId;
                    NetworkSteal = update.CanBeStolen;
                    switch (update.Action)
                    {
                        case WorldObjectAction.Unclaim:
                            // OnSteal
                            if (NetworkOwner == update.Auth.UserId)
                            {
                                NetworkOwner = string.Empty;
                                isReleased = false;
                            }
                            break;
                    }
                }
            });
        }

        private void UpdateTransform(WorldObjectUpdate update)
        {
            targetPosition = update.Object.Position.ToGodot3();
            targetRotation = update.Object.Rotation.ToGodotQuat();
            targetScale = update.Object.Size.ToGodot3();
            vel = update.Velocity.ToGodot3();
        }
    }
}