using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Hypernex.CCK.GodotVersion.Classes;
using Newtonsoft.Json.Linq;

namespace Hypernex.CCK.GodotVersion.Converters
{
    public partial class CSharpScriptConverter : IObjectConverter
    {
        public bool CanConvert(string type)
        {
            return ClassDB.IsParentClass(type, nameof(CSharpScript));
        }

        public GodotObject Convert(ConvertDB db, string type, object data)
        {
            JObject obj = (JObject)data;
            foreach (var csType in GetType().Assembly.GetTypes())
            {
                if (csType.Name != (string)obj["typename"])
                {
                    continue;
                }
                if (csType.GetInterface(nameof(ISandboxClass)) == null)
                    break;
                ScriptPathAttribute path = csType.GetCustomAttribute<ScriptPathAttribute>();
                if (path != null)
                {
                    CSharpScript script = ResourceLoader.Load<CSharpScript>(path.Path);
                    return script;
                }
            }
            return null;
        }

        public object ConvertObject(ConvertDB db, string type, GodotObject data)
        {
            Dictionary<string, JToken> dict = new Dictionary<string, JToken>();
            CSharpScript script = (CSharpScript)data;
            var obj = script.New().AsGodotObject();
            dict.Add("typename", obj.GetType().Name);
            // dict.Add("name", script.ResourceName);
            // dict.Add("path", script.ResourcePath);
            return dict;
        }
    }
}
