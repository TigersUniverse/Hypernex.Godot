using Godot;
using Hypernex.Networking.Messages.Data;

namespace Hypernex.Tools.Godot
{
    public static class VectorExtensions
    {
        public static float[] ToFloats(this Vector2 v)
        {
            return new float[] { v.X, v.Y };
        }

        public static Vector2 ToGodot2(this float[] v)
        {
            return new Vector2(v[0], v[1]);
        }

        public static float[] ToFloats(this Vector3 v)
        {
            return new float[] { v.X, v.Y, v.Z };
        }

        public static Vector3 ToGodot3(this float[] v)
        {
            return new Vector3(v[0], v[1], v[2]);
        }

        public static float3 ToFloat3(this Vector3 v)
        {
            return new float3(v.X, v.Y, v.Z);
        }

        public static Vector3 ToGodot3(this float3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public static float[] ToFloats(this Vector4 v)
        {
            return new float[] { v.X, v.Y, v.Z, v.W };
        }

        public static Vector4 ToGodot4(this float[] v)
        {
            return new Vector4(v[0], v[1], v[2], v[3]);
        }

        public static Plane ToPlane(this Vector4 v)
        {
            return new Plane(v.X, v.Y, v.Z, v.W);
        }

        public static float[] ToFloats(this Transform3D v)
        {
            return new float[]
            {
                v.Basis.Row0.X, v.Basis.Row0.Y, v.Basis.Row0.Z,
                v.Basis.Row1.X, v.Basis.Row1.Y, v.Basis.Row1.Z,
                v.Basis.Row2.X, v.Basis.Row2.Y, v.Basis.Row2.Z,
                v.Origin.X, v.Origin.Y, v.Origin.Z,
            };
        }

        public static Transform3D ToGodot3D(this float[] v)
        {
            return new Transform3D(
                new Vector3(v[0], v[1], v[2]),
                new Vector3(v[3], v[4], v[5]),
                new Vector3(v[6], v[7], v[8]),
                new Vector3(v[9], v[10], v[11])
            );
        }
    }
}
