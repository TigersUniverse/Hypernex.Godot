using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json.Linq;

namespace Hypernex.CCK.GodotVersion.Converters
{
    public partial class MaterialConverter : IObjectConverter
    {
        public bool CanConvert(string type)
        {
            return ClassDB.IsParentClass(type, nameof(StandardMaterial3D));
            return type.Equals(nameof(StandardMaterial3D), StringComparison.OrdinalIgnoreCase);
        }

        public GodotObject Convert(ConvertDB db, string type, object data)
        {
            JObject obj = (JObject)data;
            StandardMaterial3D mat = new StandardMaterial3D();
            foreach (var prop in mat.GetPropertyList())
            {
                Variant.Type vtype = prop["type"].As<Variant.Type>();
                string name = prop["name"].AsString();
                if (obj.ContainsKey(name))
                    mat.Set(name, obj[name].ToObject<SafeObject>().GetData(db));
                // mat.Set(name, SafeObject.GetDataSafe(vtype, obj[name]));
            }
            return mat;
        }

        public object ConvertObject(ConvertDB db, string type, GodotObject data)
        {
            Dictionary<string, SafeObject> dict = new Dictionary<string, SafeObject>();
            StandardMaterial3D mat = (StandardMaterial3D)data;
            foreach (var prop in mat.GetPropertyList())
            {
                Variant.Type vtype = prop["type"].As<Variant.Type>();
                string name = prop["name"].AsString();
                dict.TryAdd(name, new SafeObject(mat.Get(name), db));
            }
            return dict;
        }
    }
}
