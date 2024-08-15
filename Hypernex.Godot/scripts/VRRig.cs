using Godot;
using Hypernex.Game;
using Hypernex.Player;
using Hypernex.Tools;

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
    public XRController3D leftHandSkel;
    [Export]
    public XRController3D rightHandSkel;

    [Export]
    public Node3D openMenuWarning;

    public override void _Ready()
    {
        XRServer.WorldScale = 1d;
    }

    public override void _Process(double delta)
    {
        openMenuWarning.Visible = Init.Instance.ui.Visible && GameInstance.FocusedInstance == null;
        if (IsInstanceValid(PlayerRoot.Local))
        {
            origin.GlobalPosition = PlayerRoot.Local.Controller.GlobalPosition;
            origin.GlobalRotation = PlayerRoot.Local.Controller.GlobalRotation;
            PlayerRoot.Local.view.GlobalTransform = head.GlobalTransform;
            PlayerInputs inputs = PlayerRoot.Local.GetPart<PlayerInputs>();
            inputs.move = leftHand.GetVector2("primary") * new Vector2(1f, -1f);
            inputs.lastMouseDelta.X = -rightHand.GetVector2("primary").X * (float)delta * Mathf.Pi;
            if (IsInstanceValid(PlayerRoot.Local.Avatar))
            {
                XRServer.WorldScale = PlayerRoot.Local.Avatar.ikSystem.floorDistance;
                Transform3D floor = head.GlobalTransform;
                floor.Origin += Vector3.Down * head.Position.Y;
                // TODO: eye offsets
                PlayerRoot.Local.Avatar.ProcessIk(true, true, head.GlobalTransform.Translated(head.GlobalBasis.Z * 0.2f), floor, leftHandSkel.GlobalTransform, rightHandSkel.GlobalTransform);
                // PlayerRoot.Local.Avatar.ikSystem.head.Scale = Vector3.One * 0.01f;
            }
        }
    }
}