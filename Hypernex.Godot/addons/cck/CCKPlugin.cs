#if TOOLS
using Godot;
using System;

namespace Hypernex.CCK.Godot
{
    [Tool]
    public partial class CCKPlugin : EditorPlugin
    {
        private CCKRunEditor worldEditor;

        public override void _EnterTree()
        {
            worldEditor = new CCKRunEditor();
            AddInspectorPlugin(worldEditor);
        }

        public override void _ExitTree()
        {
            RemoveInspectorPlugin(worldEditor);
        }
    }
}
#endif
