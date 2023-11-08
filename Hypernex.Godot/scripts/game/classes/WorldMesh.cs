using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Hypernex.Tools;
using Hypernex.Tools.Godot;
using Newtonsoft.Json;

namespace Hypernex.Game.Classes
{
    [GlobalClass]
    public partial class WorldMesh : MeshInstance3D, IWorldClass
    {
        public static string ClassName => "WorldMesh";

        [JsonProperty]
        public float[] JsonTransform { get; set; }

        [JsonProperty]
        public AssetMesh Source { get; set; }

        public override void _Ready()
        {
            if (Source != null)
            {
                SurfaceTool st = new SurfaceTool();
                st.Begin(Mesh.PrimitiveType.Triangles);
                for (int i = 0; i < Source.Position.Length; i++)
                {
                    if (Source.Normal.Length != 0)
                        st.SetNormal(Source.Normal[i]);
                    if (Source.Tangent.Length != 0)
                        st.SetTangent(Source.Tangent[i].ToPlane());
                    if (Source.UV0.Length != 0)
                        st.SetUV(Source.UV0[i]);
                    if (Source.UV1.Length != 0)
                        st.SetUV(Source.UV1[i]);
                    st.AddVertex(Source.Position[i]);
                }
                for (int i = 0; i < Source.Index.Length; i++)
                {
                    st.AddIndex(Source.Index[i]);
                }
                Mesh = st.Commit();
            }
            else if (Mesh != null)
            {
                var arr = Mesh.SurfaceGetArrays(0);
                Source = new AssetMesh();
                Source.Index = arr[(int)Mesh.ArrayType.Index].AsInt32Array();
                Source.Position = arr[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                Source.Normal = arr[(int)Mesh.ArrayType.Normal].AsVector3Array();
                Source.Tangent = arr[(int)Mesh.ArrayType.Tangent].AsGodotArray<Vector4>().ToArray();
                Source.UV0 = arr[(int)Mesh.ArrayType.TexUV].AsVector2Array();
                Source.UV1 = arr[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
            }
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
            Transform = JsonTransform.ToGodot3D();
        }

        public void PreSave()
        {
            JsonTransform = Transform.ToFloats();
        }
    }
}
