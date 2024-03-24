#if TOOLS
using Godot;
// using Hypernex.Game;
// using Hypernex.Game.Classes;
// using HypernexSharp;
using System;
using System.Linq;

namespace Hypernex.CCK.GodotVersion
{
    [Tool]
    public partial class CCKPlugin : EditorPlugin
    {
        private CCKRunEditor worldEditor;
        private Control cckDock;

        public override void _EnterTree()
        {
            AddToolMenuItem("Export World", Callable.From(ExportWorld));
            // cckDock = GD.Load<PackedScene>("res://addons/cck/cck_dock.tscn").Instantiate<Control>();
            // AddControlToDock(DockSlot.LeftUr, cckDock);
            worldEditor = new CCKRunEditor();
            AddInspectorPlugin(worldEditor);
        }

        public void ExportWorld()
        {
            Node root = EditorInterface.Singleton.GetEditedSceneRoot();
            // Node desc = root.FindChildren("*").FirstOrDefault(x => x.GetType() == typeof(WorldDescriptor));
            // if (IsInstanceValid(desc))
            {
                /*
                WorldManager manager = new WorldManager();
                WorldManager.SaveToFile(manager.ConvertWorld(root), OS.GetUserDataDir(), root.Name);
                OS.ShellOpen(OS.GetUserDataDir());
                */
            }
        }

        public override void _ExitTree()
        {
            RemoveToolMenuItem("Export World");
            // RemoveControlFromDocks(cckDock);
            // cckDock.Free();
            RemoveInspectorPlugin(worldEditor);
        }
    }
}
#endif
