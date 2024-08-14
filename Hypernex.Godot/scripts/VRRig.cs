using Godot;
using Hypernex.Game;

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
        XRServer.WorldScale = 1.5d;
    }

    public override void _Process(double delta)
    {
        openMenuWarning.Visible = Init.Instance.ui.Visible;
        if (IsInstanceValid(PlayerRoot.Local))
        {
            origin.GlobalPosition = PlayerRoot.Local.Controller.GlobalPosition;
            origin.GlobalRotation = PlayerRoot.Local.Controller.GlobalRotation;
            if (IsInstanceValid(PlayerRoot.Local.Avatar))
            {
                PlayerRoot.Local.Avatar.ProcessIk(true, head.GlobalTransform, leftHand.GlobalTransform, rightHand.GlobalTransform);
            }
        }
    }
}