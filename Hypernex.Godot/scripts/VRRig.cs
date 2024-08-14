using Godot;
using Hypernex.Game;
using Hypernex.Player;

public partial class VRRig : Node3D
{
    [Export]
    public XROrigin3D origin;
    [Export]
    public XRCamera3D head;
    [Export]
    public XRController3D leftHand;
    [Export]
    public XRController3D rightHand;

    [Export]
    public Node3D openMenuWarning;

    public override void _Ready()
    {
        XRServer.WorldScale = 1d;
    }

    public override void _Process(double delta)
    {
        openMenuWarning.Visible = Init.Instance.ui.Visible;
        if (IsInstanceValid(PlayerRoot.Local))
        {
            origin.GlobalPosition = PlayerRoot.Local.Controller.GlobalPosition;
            origin.GlobalRotation = PlayerRoot.Local.Controller.GlobalRotation;
            PlayerInputs inputs = PlayerRoot.Local.GetPart<PlayerInputs>();
            inputs.move = leftHand.GetVector2("primary") * new Vector2(1f, -1f);
            inputs.lastMouseDelta.X = -rightHand.GetVector2("primary").X * (float)delta * Mathf.Pi;
            if (IsInstanceValid(PlayerRoot.Local.Avatar))
            {
                PlayerRoot.Local.Avatar.ProcessIk(true, true, head.GlobalTransform, leftHand.GlobalTransform, rightHand.GlobalTransform);
                PlayerRoot.Local.Avatar.ikSystem.head.Scale = Vector3.One * 0.01f;
            }
        }
    }
}