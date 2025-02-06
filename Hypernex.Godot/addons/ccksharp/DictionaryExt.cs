using Godot;
using Godot.Collections;

namespace Hypernex.CCK.GodotVersion
{
    public static class DictionaryExt
    {
        public static bool TryGetDict(this Dictionary dict, string key, out Dictionary value)
        {
            value = default;
            bool b = dict.TryGetValue(key, out Variant val) && val.VariantType == Variant.Type.Dictionary;
            if (b)
                value = val.AsGodotDictionary();
            return b;
        }

        public static bool TryGetFloat(this Dictionary dict, string key, out float value)
        {
            value = default;
            bool b = dict.TryGetValue(key, out Variant val) && (val.VariantType == Variant.Type.Int || val.VariantType == Variant.Type.Float);
            if (b)
                value = val.AsSingle();
            return b;
        }

        public static bool TryGetInt32(this Dictionary dict, string key, out int value)
        {
            value = default;
            bool b = dict.TryGetValue(key, out Variant val) && (val.VariantType == Variant.Type.Int || val.VariantType == Variant.Type.Float);
            if (b)
                value = val.AsInt32();
            return b;
        }

        public static bool TryGetArray(this Dictionary dict, string key, out Array value)
        {
            value = default;
            bool b = dict.TryGetValue(key, out Variant val) && (val.VariantType == Variant.Type.Array);
            if (b)
                value = val.AsGodotArray();
            return b;
        }

        public static bool TryGetString(this Dictionary dict, string key, out string value)
        {
            value = default;
            bool b = dict.TryGetValue(key, out Variant val) && val.VariantType == Variant.Type.String;
            if (b)
                value = val.AsString();
            return b;
        }

        public static bool TryGetBool(this Dictionary dict, string key, out bool value)
        {
            value = default;
            bool b = dict.TryGetValue(key, out Variant val) && val.VariantType == Variant.Type.Bool;
            if (b)
                value = val.AsBool();
            return b;
        }
    }
}
