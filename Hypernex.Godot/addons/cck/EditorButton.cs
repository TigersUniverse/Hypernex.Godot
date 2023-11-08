#if TOOLS
using Godot;
using System;
using System.Reflection;

namespace Hypernex.CCK.Godot
{
    public partial class EditorButton : Button
    {
        public MethodInfo methodInfo;
        public GodotObject target;

        public override void _Ready()
        {
            Text = methodInfo.Name;
            Pressed += Clicked;
        }

        private void Clicked()
        {
            methodInfo.Invoke(target, new object[0]);
        }
    }
}
#endif
