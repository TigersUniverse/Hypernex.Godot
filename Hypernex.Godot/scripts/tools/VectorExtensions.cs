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
    }
}
