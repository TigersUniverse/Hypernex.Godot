using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Hypernex.CCK.GodotVersion.Converters
{
    public partial class ConvertDB
    {
        public readonly List<IObjectConverter> converters = new List<IObjectConverter>();

        public void Register<T>() where T : IObjectConverter, new()
        {
            converters.Add(new T());
        }

        public bool CanConvert(string type)
        {
            return converters.Any(x => x.CanConvert(type));
        }

        public object ConvertObject(string type, GodotObject data)
        {
            return converters.FirstOrDefault(x => x.CanConvert(type))?.ConvertObject(this, type, data) ?? null;
        }

        public GodotObject Convert(string type, object data)
        {
            return converters.FirstOrDefault(x => x.CanConvert(type))?.Convert(this, type, data) ?? null;
        }
    }
}
