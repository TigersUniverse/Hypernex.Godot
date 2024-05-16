using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json.Linq;

namespace Hypernex.CCK.GodotVersion.Converters
{
    public partial class Texture2DConverter : IObjectConverter
    {
        public bool CanConvert(string type)
        {
            return ClassDB.IsParentClass(type, nameof(Texture2D));
        }

        public GodotObject Convert(ConvertDB db, string type, object data)
        {
            JObject obj = (JObject)data;
            Image img = Image.CreateFromData((int)(long)obj["width"], (int)(long)obj["height"], (bool)obj["mipmaps"], (Image.Format)(long)obj["format"], obj["data"].ToObject<byte[]>());
            ImageTexture tex = ImageTexture.CreateFromImage(img);
            return tex;
        }

        public object ConvertObject(ConvertDB db, string type, GodotObject data)
        {
            Dictionary<string, JToken> dict = new Dictionary<string, JToken>();
            Texture2D tex = (Texture2D)data;
            Image img = tex.GetImage();
            dict.Add("width", img.GetWidth());
            dict.Add("height", img.GetHeight());
            dict.Add("mipmaps", img.HasMipmaps());
            dict.Add("format", (long)img.GetFormat());
            dict.Add("data", img.GetData());
            return dict;
        }
    }
}
