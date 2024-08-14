using Godot;

namespace Hypernex.CCK.GodotVersion.Converters
{
    public partial interface IObjectConverter
    {
        bool CanConvert(string type);
        object ConvertObject(ConvertDB db, string type, GodotObject data);
        GodotObject Convert(ConvertDB db, string type, object data);
    }
}
