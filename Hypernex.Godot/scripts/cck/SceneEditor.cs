using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Hypernex.CCK.GodotVersion
{
    public partial class SceneEditor : Node
    {
        [Export]
        public Tree tree;
        [Export]
        public EditorEntity rootEntity;
        [Export]
        public PopupMenu contextMenu;

        public Dictionary<EditorEntity, TreeItem> entItemLookup = new Dictionary<EditorEntity, TreeItem>();

        public override void _EnterTree()
        {
            tree.ItemMouseSelected += Clicked;
            tree.ItemEdited += TreeEdited;
            contextMenu.IdPressed += ContextSelected;
            RefreshScene();
        }

        public override void _ExitTree()
        {
            tree.ItemMouseSelected -= Clicked;
            tree.ItemEdited -= TreeEdited;
            contextMenu.IdPressed -= ContextSelected;
        }

        public void RefreshScene()
        {
            entItemLookup.Clear();
            tree.Clear();
            if (IsInstanceValid(rootEntity))
            {
                var root = tree.CreateItem();
                root.SetText(0, "Scene");
                FillSceneTree(rootEntity, root);
            }
        }

        public EditorEntity AddEntity(EditorEntity parent = null)
        {
            var ent = new EditorEntity();
            ent.Name = "New Entity";
            if (IsInstanceValid(parent))
                parent.AddChild(ent, true);
            else
                rootEntity.AddChild(ent, true);
            var treeItem = GetTreeItem(parent).CreateChild();
            FillSceneTree(ent, treeItem);
            return ent;
        }

        public void RemoveEntity(EditorEntity ent)
        {
        }

        public TreeItem GetTreeItem(EditorEntity ent)
        {
            return entItemLookup[ent];
        }

        private void FillSceneTree(EditorEntity ent, TreeItem entItem)
        {
            entItem.SetText(0, ent.Name);
            entItem.SetEditable(0, true);
            entItem.SetMetadata(0, ent);
            entItemLookup.Add(ent, entItem);
            foreach (var ch in ent.GetChildEnts())
            {
                var item = entItem.CreateChild();
                FillSceneTree(ch, item);
            }
        }

        private void ContextSelected(long id)
        {
            var item = tree.GetSelected();
            var meta = item.GetMetadata(0).As<EditorEntity>();
            switch (id)
            {
                case 0: // new entity
                    if (IsInstanceValid(meta))
                    {
                        AddEntity(meta);
                    }
                    break;
                case 1: // delete entity
                    if (IsInstanceValid(meta))
                    {
                        RemoveEntity(meta);
                    }
                    break;
            }
        }

        private void TreeEdited()
        {
            var item = tree.GetEdited();
            var meta = item.GetMetadata(0).As<EditorEntity>();
            meta.Name = item.GetText(0);
            item.SetText(0, meta.Name);
        }

        private void Clicked(Vector2 pos, long mouse)
        {
            if (mouse == 2)
            {
                var item = tree.GetSelected();
                contextMenu.Position = DisplayServer.MouseGetPosition();
                contextMenu.Popup();
            }
        }
    }
}