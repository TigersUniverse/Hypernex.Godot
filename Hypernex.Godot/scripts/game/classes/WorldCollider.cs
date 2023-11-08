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
    public partial class WorldCollider : CollisionShape3D, IWorldClass
    {
        public static string ClassName => "WorldCollider";

        [JsonProperty]
        public float[] JsonTransform { get; set; }

        [JsonProperty]
        public AssetCollider Source { get; set; }

        public override void _Ready()
        {
            if (Source != null)
            {
                List<Vector3> arr = new List<Vector3>();
                /*for (int i = 0; i < Source.Index.Length / 3; i++)
                {
                    arr.Add(Source.Position[Source.Index[i * 3 + 0]]);
                    arr.Add(Source.Position[Source.Index[i * 3 + 1]]);
                    arr.Add(Source.Position[Source.Index[i * 3 + 2]]);
                }*/
                ConcavePolygonShape3D mesh = new ConcavePolygonShape3D();
                mesh.Data = Source.Position.ToArray();
                Shape = mesh;
            }
            else if (Shape != null && Shape is ConcavePolygonShape3D mesh)
            {
                Source = new AssetCollider();
                Source.Position = mesh.Data.ToArray();
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
