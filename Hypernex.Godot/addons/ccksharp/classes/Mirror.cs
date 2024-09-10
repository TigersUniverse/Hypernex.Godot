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
        private ShaderMaterial mat;

        [Export]
        public MeshInstance3D existingMesh;

        public Func<List<Node>> CallbackGetNodes;

        public override void _EnterTree()
        {
            xr = XRServer.FindInterface("OpenXR");
            mat = new ShaderMaterial()
            {
                Shader = GD.Load<Shader>("res://shaders/mirror.gdshader"),
            };
            if (IsInstanceValid(existingMesh))
            {
                var s2 = existingMesh.Mesh.GetAabb();
                var s = s2.Size;
                var max = (int)s.MaxAxisIndex();
                var min = (int)s.MinAxisIndex();
                var mid = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (i != max && i != min)
                    {
                        mid = i;
                        break;
                    }
                }
                GlobalBasis = GlobalBasis.Scaled(Vector3.One / GlobalBasis.Scale);
                size = new Vector2(s[max], s[mid]) * existingMesh.GlobalBasis.Scale.X;
            }
            // else
            {
                mesh = new MeshInstance3D()
                {
                    Mesh = new QuadMesh()
                    {
                        Size = size,
                    },
                    MaterialOverride = mat,
                };
                AddChild(mesh);
            }
            leftVp = new SubViewport();
            AddChild(leftVp);
            rightVp = new SubViewport();
            AddChild(rightVp);
            leftCam = new Camera3D();
            leftVp.AddChild(leftCam);
            rightCam = new Camera3D();
            rightVp.AddChild(rightCam);
            mat.SetShaderParameter("mirror_tex_left", leftVp.GetTexture());
            mat.SetShaderParameter("mirror_tex_right", rightVp.GetTexture());
            RenderingServer.FramePreDraw += Update;
        }

        public override void _Ready()
        {
            _ExitTree();
            _EnterTree();
        }

        public override void _ExitTree()
        {
            if (IsInstanceValid(mesh))
                mesh.QueueFree();
            if (IsInstanceValid(leftVp))
                leftVp.QueueFree();
            if (IsInstanceValid(rightVp))
                rightVp.QueueFree();
            if (IsInstanceValid(leftCam))
                leftCam.QueueFree();
            if (IsInstanceValid(rightCam))
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
            if (cam is XRCamera3D)
            {
                return (int)xr.GetRenderTargetSize().X;
            }
            return (int)GetTree().Root.Size.X;
        }

        public void MoveCamera(SubViewport vp, Camera3D cam, uint idx, Camera3D realCamera)
        {
            // vp.Size = (Vector2I)(Vector2.One * GetCameraSize(realCamera));
            // vp.Size = (Vector2I)(size.LimitLength(size.X / size.Y) * GetCameraSize(realCamera));
            vp.Size = (Vector2I)(size.Normalized() * GetCameraSize(realCamera)) * 2;
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
            cam.ForceUpdateTransform();
            // cam.SetFrustum(size.X, frustum_offset, near, realCamera.Far);
            cam.SetFrustum(size.Y, frustum_offset, near, realCamera.Far);
        }

        public override void _Process(double delta)
        {
            // Update();
        }

        public override void _PhysicsProcess(double delta)
        {
            // Update();
        }

        public void Update()
        {
            if (!IsInstanceValid(realCamera))
            {
                realCamera = GetViewport().GetCamera3D();
                return;
            }
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
