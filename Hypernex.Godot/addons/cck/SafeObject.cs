using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.CCK.GodotVersion.Converters;
using Hypernex.Tools;
using Newtonsoft.Json.Linq;

namespace Hypernex.CCK.GodotVersion
{
    public partial class SafeObject
    {
        public static Variant GetDataSafe(Variant.Type type, object data)
        {
            switch (type)
            {
                default:
                    return default;
                case Variant.Type.Bool:
                    return Variant.From((bool)data);
                case Variant.Type.Int:
                    return Variant.From((int)(long)data);
                case Variant.Type.Float:
                    return Variant.From((float)(double)data);
                case Variant.Type.String:
                    return Variant.From((string)data);
                case Variant.Type.Vector2:
                    return Variant.From(((JObject)data).ToObject<Vector2>());
                case Variant.Type.Vector2I:
                    return Variant.From(((JObject)data).ToObject<Vector2I>());
                case Variant.Type.Rect2:
                    return Variant.From(((JObject)data).ToObject<Rect2>());
                case Variant.Type.Rect2I:
                    return Variant.From(((JObject)data).ToObject<Rect2I>());
                case Variant.Type.Vector3:
                    return Variant.From(((JObject)data).ToObject<Vector3>());
                case Variant.Type.Vector3I:
                    return Variant.From(((JObject)data).ToObject<Vector3I>());
                case Variant.Type.Transform2D:
                    return Variant.From(((JObject)data).ToObject<Transform2D>());
                case Variant.Type.Vector4:
                    return Variant.From(((JObject)data).ToObject<Vector4>());
                case Variant.Type.Vector4I:
                    return Variant.From(((JObject)data).ToObject<Vector4I>());
                case Variant.Type.Plane:
                    return Variant.From(((JObject)data).ToObject<Plane>());
                case Variant.Type.Quaternion:
                    return Variant.From(((JObject)data).ToObject<Quaternion>());
                case Variant.Type.Aabb:
                    return Variant.From(((JObject)data).ToObject<Aabb>());
                case Variant.Type.Basis:
                    return Variant.From(((JObject)data).ToObject<Basis>());
                case Variant.Type.Transform3D:
                    return Variant.From(((JObject)data).ToObject<Transform3D>());
                case Variant.Type.Projection:
                    return Variant.From(((JObject)data).ToObject<Projection>());
                case Variant.Type.Color:
                    return Variant.From(((JObject)data).ToObject<Color>());
                case Variant.Type.StringName:
                    return Variant.From(((JObject)data).ToObject<StringName>());
                case Variant.Type.NodePath:
                    return Variant.From(((JObject)data).ToObject<NodePath>());
                case Variant.Type.Rid:
                    return Variant.From(((JObject)data).ToObject<Rid>());
                case Variant.Type.Callable:
                    return default;
                case Variant.Type.Signal:
                    return default;
                case Variant.Type.Dictionary:
                    return Variant.From(((JObject)data).ToObject<global::Godot.Collections.Dictionary>());
                case Variant.Type.Array:
                    return Variant.From(((JArray)data).ToObject<global::Godot.Collections.Array>());
                case Variant.Type.PackedByteArray:
                    return Variant.From(((JArray)data).ToObject<byte[]>());
                case Variant.Type.PackedInt32Array:
                    return Variant.From(((JArray)data).ToObject<int[]>());
                case Variant.Type.PackedInt64Array:
                    return Variant.From(((JArray)data).ToObject<long[]>());
                case Variant.Type.PackedFloat32Array:
                    return Variant.From(((JArray)data).ToObject<float[]>());
                case Variant.Type.PackedFloat64Array:
                    return Variant.From(((JArray)data).ToObject<double[]>());
                case Variant.Type.PackedStringArray:
                    return Variant.From(((JArray)data).ToObject<string[]>());
                case Variant.Type.PackedVector2Array:
                    return Variant.From(((JArray)data).ToObject<Vector2[]>());
                case Variant.Type.PackedVector3Array:
                    return Variant.From(((JArray)data).ToObject<Vector3[]>());
                case Variant.Type.PackedColorArray:
                    return Variant.From(((JArray)data).ToObject<Color[]>());
            }
        }

        public int vType;
        public string oType;
        public object data;

        public SafeObject()
        {
        }

        public SafeObject(Variant v, ConvertDB convertDb)
        {
            vType = (int)v.VariantType;
            if (v.VariantType == Variant.Type.Object)
            {
                if (v.Obj == null)
                {
                    oType = null;
                    data = null;
                    return;
                }
                oType = v.AsGodotObject().GetClass();
                data = convertDb.ConvertObject(oType, v.AsGodotObject());
            }
            else
            {
                data = v.Obj;
                switch (v.VariantType)
                {
                    case Variant.Type.PackedByteArray:
                        data = v.AsByteArray();
                        break;
                }
            }
        }

        public Variant GetData(ConvertDB convertDb)
        {
            if (vType == (int)Variant.Type.Object)
            {
                if (!string.IsNullOrEmpty(oType) && convertDb.CanConvert(oType))
                {
                    return convertDb.Convert(oType, data);
                }
                return default;
            }
            return GetDataSafe((Variant.Type)vType, data);
        }
    }
}
