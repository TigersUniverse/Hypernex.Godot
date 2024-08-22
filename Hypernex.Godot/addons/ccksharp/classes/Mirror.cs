using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// based off https://github.com/Norodix/GodotMirror/tree/godot4
namespace Hypernex.CCK.GodotVersion.Classes
{
    public partial class Mirror : Node3D, ISandboxClass
    {
        public const string TypeName = "Mirror";

        [Export]
        public Vector2 size;

        public Camera3D realCamera;
        public float resolutionPerUnit = 200f;

        private MeshInstance3D mesh;
        private XRInterface xr;
        private SubViewport leftVp;
        private SubViewport rightVp;
        private Camera3D leftCam;
        private Camera3D rightCam;

        public Func<List<Node>> CallbackGetNodes;

        public override void _EnterTree()
        {
            xr = XRServer.FindInterface("OpenXR");
            mesh = new MeshInstance3D()
            {
                Mesh = new QuadMesh()
                {
                    Size = size,
                },
                MaterialOverride = new ShaderMaterial()
                {
                    Shader = GD.Load<Shader>("res://shaders/mirror.gdshader"),
                },
            };
            AddChild(mesh);
            leftVp = new SubViewport();
            AddChild(leftVp);
            rightVp = new SubViewport();
            AddChild(rightVp);
            leftCam = new Camera3D();
            leftVp.AddChild(leftCam);
            rightCam = new Camera3D();
            rightVp.AddChild(rightCam);
            ((ShaderMaterial)mesh.MaterialOverride).SetShaderParameter("mirror_tex_left", leftVp.GetTexture());
            ((ShaderMaterial)mesh.MaterialOverride).SetShaderParameter("mirror_tex_right", rightVp.GetTexture());
            RenderingServer.FramePreDraw += Update;
        }

        public override void _ExitTree()
        {
            mesh.QueueFree();
            leftVp.QueueFree();
            rightVp.QueueFree();
            leftCam.QueueFree();
            rightCam.QueueFree();
            RenderingServer.FramePreDraw -= Update;
        }

        public Transform3D GetPositionOfCamera(Camera3D cam, uint view)
        {
            if (cam is XRCamera3D xrCam)
            {
                return xr.GetTransformForView(view, xrCam.GetParent<Node3D>().GlobalTransform);
            }
            return cam.GlobalTransform;
        }

        public int GetCameraSize(Camera3D cam)
        {
            if (cam is XRCamera3D xrCam)
            {
                return (int)xr.GetRenderTargetSize().X;
            }
            return (int)GetTree().Root.Size.X;
        }

        public void MoveCamera(SubViewport vp, Camera3D cam, uint idx, Camera3D realCamera)
        {
            vp.Size = Vector2I.One * GetCameraSize(realCamera);
            var normal = GlobalBasis.Z;
            var xform = MirrorTransform(normal, GlobalPosition);
            cam.GlobalTransform = xform * GetPositionOfCamera(realCamera, idx);

            cam.GlobalTransform = cam.GlobalTransform.LookingAt(
                cam.GlobalTransform.Origin/2 + GetPositionOfCamera(realCamera, idx).Origin/2,
                GlobalBasis.Y
            );
            var cam2MirrorOffset = GlobalPosition - cam.GlobalPosition;
            var near = Mathf.Abs(cam2MirrorOffset.Dot(normal));
            near += 0.05f;

            var cam2mirror_camlocal = cam.GlobalBasis.Inverse() * cam2MirrorOffset;
            var frustum_offset = new Vector2(cam2mirror_camlocal.X, cam2mirror_camlocal.Y);
            cam.SetFrustum(size.X, frustum_offset, near, realCamera.Far);
        }

        public override void _Process(double delta)
        {
            Update();
        }

        public override void _PhysicsProcess(double delta)
        {
            Update();
        }

        public void Update()
        {
            if (!IsInstanceValid(realCamera))
                return;
            MoveCamera(leftVp, leftCam, 0, realCamera);
            MoveCamera(rightVp, rightCam, 1, realCamera);
        }

        private Transform3D MirrorTransform(Vector3 n, Vector3 d)
        {
            var x = new Vector3(1, 0, 0) - 2 * new Vector3(n.X * n.X, n.X * n.Y, n.X * n.Z);
            var y = new Vector3(0, 1, 0) - 2 * new Vector3(n.Y * n.X, n.Y * n.Y, n.Y * n.Z);
            var z = new Vector3(0, 0, 1) - 2 * new Vector3(n.Z * n.X, n.Z * n.Y, n.Z * n.Z);

            var offset = 2 * n.Dot(d) * n;
            return new Transform3D(new Basis(x, y, z), offset);
        }
    }
}
