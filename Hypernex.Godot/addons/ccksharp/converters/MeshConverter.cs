using System;
using Godot;
using Newtonsoft.Json.Linq;

namespace Hypernex.CCK.GodotVersion.Converters
{
    public partial class MeshConverter : IObjectConverter
    {
        public partial struct MeshStruct
        {
            public Vector3[] Vertex;
            public Vector3[] Normal;
            public float[] Tangent;
            public Color[] Color;
            public Vector2[] TexUV;
            public Vector2[] TexUV2;
            public int[] Index;

            public MeshStruct(global::Godot.Collections.Array arrs)
            {
                Vertex = arrs[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                Normal = arrs[(int)Mesh.ArrayType.Normal].AsVector3Array();
                Tangent = arrs[(int)Mesh.ArrayType.Tangent].AsFloat32Array();
                Color = arrs[(int)Mesh.ArrayType.Color].AsColorArray();
                TexUV = arrs[(int)Mesh.ArrayType.TexUV].AsVector2Array();
                TexUV2 = arrs[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
                Index = arrs[(int)Mesh.ArrayType.Index].AsInt32Array();
            }

            public global::Godot.Collections.Array GetArrays()
            {
                var arrs = new global::Godot.Collections.Array();
                arrs.Resize((int)Mesh.ArrayType.Max);
                if (Vertex.Length != 0)
                    arrs[(int)Mesh.ArrayType.Vertex] = Vertex;
                if (Normal.Length != 0)
                    arrs[(int)Mesh.ArrayType.Normal] = Normal;
                if (Tangent.Length != 0)
                    arrs[(int)Mesh.ArrayType.Tangent] = Tangent;
                if (Color.Length != 0)
                    arrs[(int)Mesh.ArrayType.Color] = Color;
                if (TexUV.Length != 0)
                    arrs[(int)Mesh.ArrayType.TexUV] = TexUV;
                if (TexUV2.Length != 0)
                    arrs[(int)Mesh.ArrayType.TexUV2] = TexUV2;
                if (Index.Length != 0)
                    arrs[(int)Mesh.ArrayType.Index] = Index;
                return arrs;
            }
        }

        public bool CanConvert(string type)
        {
            return type.Equals(nameof(ArrayMesh), StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertObject(ConvertDB db, string type, GodotObject data)
        {
            ArrayMesh mesh = (ArrayMesh)data;
            int count = mesh.GetSurfaceCount();
            MeshStruct[] surfaces = new MeshStruct[count];
            for (int i = 0; i < count; i++)
            {
                var arrs = mesh.SurfaceGetArrays(i);
                surfaces[i] = new MeshStruct(arrs);
            }
            return surfaces;
        }

        public GodotObject Convert(ConvertDB db, string type, object data)
        {
            ArrayMesh mesh = new ArrayMesh();
            MeshStruct[] array = ((JArray)data).ToObject<MeshStruct[]>();
            for (int i = 0; i < array.Length; i++)
            {
                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array[i].GetArrays());
            }
            return mesh;
        }
    }
}
