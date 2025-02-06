using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hypernex.Game;

namespace Hypernex.CCK.GodotVersion
{
    public partial class SceneEditor : Node
    {
        [Export]
        public Tree tree;
        [Export]
        public PopupMenu contextMenu;

        [Export]
        public Node rootEntity;
        public Dictionary<Node, TreeItem> entItemLookup = new Dictionary<Node, TreeItem>();

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
                FillSceneTree(rootEntity as IEntity, root);
            }
        }

        public IEntity AddEntity(IEntity parent = null)
        {
            var ent = new Entity3D();
            ent.Name = "New Entity";
            if (parent != null)
                parent.AsNode.AddChild(ent, true);
            else
                rootEntity.AddChild(ent, true);
            var treeItem = GetTreeItem(parent).CreateChild();
            FillSceneTree(ent, treeItem);
            return ent;
        }

        public void RemoveEntity(IEntity ent)
        {
            if (ent.ParentEnt == null)
                return;
            GetTreeItem(ent).Free();
            ent.AsNode.QueueFree();
        }

        public TreeItem GetTreeItem(IEntity ent)
        {
            return entItemLookup[ent.AsNode];
        }

        private void FillSceneTree(IEntity ent, TreeItem entItem)
        {
            entItem.SetText(0, ent.Name);
            entItem.SetEditable(0, true);
            entItem.SetMetadata(0, ent.AsNode);
            entItemLookup.Add(ent.AsNode, entItem);
            foreach (var ch in ent.GetChildEnts())
            {
                var item = entItem.CreateChild();
                FillSceneTree(ch, item);
            }
        }

        private void ContextSelected(long id)
        {
            var item = tree.GetSelected();
            var meta = item.GetMetadata(0).As<Node>();
            switch (id)
            {
                case 0: // new entity
                    if (IsInstanceValid(meta))
                    {
                        AddEntity(meta as IEntity);
                    }
                    break;
                case 1: // delete entity
                    if (IsInstanceValid(meta))
                    {
                        RemoveEntity(meta as IEntity);
                    }
                    break;
            }
        }

        private void TreeEdited()
        {
            var item = tree.GetEdited();
            var meta = item.GetMetadata(0).As<Node>();
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