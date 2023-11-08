#if TOOLS
using Godot;
using System;
using System.Reflection;

namespace Hypernex.CCK.Godot
{
    [Tool]
    public partial class CCKRunEditor : EditorInspectorPlugin
    {
        public override bool _CanHandle(GodotObject @object)
        {
            return true;
        }

        public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
        {
            if (name.Length <= 1)
            {
                return false;
            }
            MethodInfo members = @object.GetType().GetMethod(name.Replace("_", ""), BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[0]);
            if (members != null)
            {
                var btn = new EditorButton();
                btn.methodInfo = members;
                btn.target = @object;
                AddCustomControl(btn);
            }
            return members != null;
        }
    }
}
#endif
