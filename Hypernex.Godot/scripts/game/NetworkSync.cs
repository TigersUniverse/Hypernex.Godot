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
        public bool NetworkSteal;
        public Vector3 targetPosition;
        public Quaternion targetRotation;
        public Vector3 targetScale;
        public Vector3 vel;

        public bool IsOwnedByLocalPlayer() => APITools.CurrentUser == null || NetworkOwner == APITools.CurrentUser?.Id;

        public override void _Ready()
        {
            parent = GetParent<Node3D>();
            targetPosition = parent.Position;
            targetRotation = parent.Quaternion;
            targetScale = parent.Scale;
        }

        private async void UpdateLoop()
        {
            while (IsInstanceValid(this))
            {
                await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
                if (world.gameInstance.IsOpen && IsOwnedByLocalPlayer())
                {
                    world.gameInstance.SendMessage(new WorldObjectUpdate()
                    {
                        Auth = GetJoinAuth(),
                        Action = WorldObjectAction.Update,
                        CanBeStolen = CanSteal,
                        Object = new NetworkedObject()
                        {
                            ObjectLocation = world.GetLocalPath(parent),
                            Position = parent.Position.ToFloat3(),
                            Rotation = parent.Quaternion.ToFloat4(),
                            Size = parent.Scale.ToFloat3(),
                            IgnoreObjectLocation = false,
                        },
                        // Velocity = vel,
                    }, Nexport.MessageChannel.Unreliable);
                }
            }
        }

        public JoinAuth GetJoinAuth()
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
                parent.Scale = parent.Scale.Slerp(targetScale, (float)delta * lerpSpeed);
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
                }
            });
        }

        public void UpdateTransform(WorldObjectUpdate update)
        {
            targetPosition = update.Object.Position.ToGodot3();
            targetRotation = update.Object.Rotation.ToGodotQuat();
            targetScale = update.Object.Size.ToGodot3();
            vel = update.Velocity.ToGodot3();
        }
    }
}