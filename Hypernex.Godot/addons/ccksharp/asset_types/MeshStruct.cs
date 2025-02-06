using Godot;

namespace Hypernex.CCK.GodotVersion.AssetTypes
{
    public class MeshStruct : EntityAssetData
    {
        public MeshSurfaceStruct[] Surfaces;

        public MeshStruct()
        {
        }

        public MeshStruct(Mesh mesh)
        {
            Surfaces = new MeshSurfaceStruct[mesh.GetSurfaceCount()];
            for (int i = 0; i < mesh.GetSurfaceCount(); i++)
            {
                Godot.Collections.Array arr = mesh.SurfaceGetArrays(i);
                Surfaces[i] = new MeshSurfaceStruct(arr);
            }
        }

        public ArrayMesh GetMesh()
        {
            ArrayMesh mesh = new ArrayMesh();
            for (int i = 0; i < Surfaces.Length; i++)
                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Surfaces[i].GetArrays());
            return mesh;
        }
    }

    public struct MeshSurfaceStruct
    {
        public Vector3[] Vertex;
        public Vector3[] Normal;
        public float[] Tangent;
        public Color[] Color;
        public Vector2[] TexUV;
        public Vector2[] TexUV2;
        public int[] Index;

        public MeshSurfaceStruct(Godot.Collections.Array arrs)
        {
            Vertex = arrs[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            Normal = arrs[(int)Mesh.ArrayType.Normal].AsVector3Array();
            Tangent = arrs[(int)Mesh.ArrayType.Tangent].AsFloat32Array();
            Color = arrs[(int)Mesh.ArrayType.Color].AsColorArray();
            TexUV = arrs[(int)Mesh.ArrayType.TexUV].AsVector2Array();
            TexUV2 = arrs[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
            Index = arrs[(int)Mesh.ArrayType.Index].AsInt32Array();
        }

        public Godot.Collections.Array GetArrays()
        {
            var arrs = new Godot.Collections.Array();
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
}