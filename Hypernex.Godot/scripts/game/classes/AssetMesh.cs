using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using Newtonsoft.Json;

namespace Hypernex.Game.Classes
{
    public partial class AssetMesh : WorldAsset
    {
        [JsonProperty]
        [Export]
        public int[] Index { get; set; } = Array.Empty<int>();
        [Export]
        public Vector3[] Position { get; set; } = Array.Empty<Vector3>();
        [Export]
        public Vector3[] Normal { get; set; } = Array.Empty<Vector3>();
        public Vector4[] Tangent { get; set; } = Array.Empty<Vector4>();
        [Export]
        public Vector2[] UV0 { get; set; } = Array.Empty<Vector2>();
        [Export]
        public Vector2[] UV1 { get; set; } = Array.Empty<Vector2>();

        [JsonProperty]
        public float[][] FPosition
        {
            get => Position.Select(x => x.ToFloats()).ToArray();
            set => Position = value.Select(x => x.ToGodot3()).ToArray();
        }

        [JsonProperty]
        public float[][] FNormal
        {
            get => Normal.Select(x => x.ToFloats()).ToArray();
            set => Normal = value.Select(x => x.ToGodot3()).ToArray();
        }

        [JsonProperty]
        public float[][] FTangent
        {
            get => Tangent.Select(x => x.ToFloats()).ToArray();
            set => Tangent = value.Select(x => x.ToGodot4()).ToArray();
        }

        [JsonProperty]
        public float[][] FUV0
        {
            get => UV0.Select(x => x.ToFloats()).ToArray();
            set => UV0 = value.Select(x => x.ToGodot2()).ToArray();
        }

        [JsonProperty]
        public float[][] FUV1
        {
            get => UV1.Select(x => x.ToFloats()).ToArray();
            set => UV1 = value.Select(x => x.ToGodot2()).ToArray();
        }

        public Mesh ToMesh()
        {
            SurfaceTool st = new SurfaceTool();
            st.Begin(Mesh.PrimitiveType.Triangles);
            for (int i = 0; i < Position.Length; i++)
            {
                if (Normal.Length != 0)
                    st.SetNormal(Normal[i]);
                if (Tangent.Length != 0)
                    st.SetTangent(Tangent[i].ToPlane());
                if (UV0.Length != 0)
                    st.SetUV(UV0[i]);
                if (UV1.Length != 0)
                    st.SetUV(UV1[i]);
                st.AddVertex(Position[i]);
            }
            for (int i = 0; i < Index.Length; i++)
            {
                st.AddIndex(Index[i]);
            }
            return st.Commit();
        }

        public AssetMesh FromMesh(Mesh mesh)
        {
            Name = mesh.ResourceName;
            Path = mesh.ResourcePath;
            var arr = mesh.SurfaceGetArrays(0);
            Index = arr[(int)Mesh.ArrayType.Index].AsInt32Array();
            Position = arr[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            Normal = arr[(int)Mesh.ArrayType.Normal].AsVector3Array();
            Tangent = arr[(int)Mesh.ArrayType.Tangent].AsGodotArray<Vector4>().ToArray();
            UV0 = arr[(int)Mesh.ArrayType.TexUV].AsVector2Array();
            UV1 = arr[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
            return this;
        }

        public void LoadFromData(string data)
        {
            JsonTools.JsonPopulate(data, this);
        }

        public string SaveToData()
        {
            return JsonTools.JsonSerialize(this);
        }

        public void PostLoad()
        {
        }

        public void PreSave()
        {
        }
    }
}
