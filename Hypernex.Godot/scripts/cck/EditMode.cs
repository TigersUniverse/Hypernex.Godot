using System;
using System.Linq;
using Godot;

namespace Hypernex.CCK.GodotVersion
{
    public partial class EditMode : Node
    {
        [Export]
        public PanelContainer menuContainer;
        [Export]
        public MenuBar menuBar;
        [Export]
        public Tree assetsTree;
        [Export]
        public FileDialog openFile;
        [Export]
        public PopupMenu fileMenu;
        [Export]
        public PopupMenu assetsMenu;

        public string workingDirectory = string.Empty;

        public override void _EnterTree()
        {
            GetWindow().FilesDropped += DroppedFiles;
            fileMenu.IdPressed += FileMenuPressed;
            assetsMenu.IdPressed += AssetsMenuPressed;
            openFile.DirSelected += OpenDirSelected;
            openFile.FilesSelected += OpenFileSelected;
            RefreshAssets();

            bool isDarkMode = DisplayServer.IsDarkMode();
            var box = new StyleBoxFlat()
            {
                BgColor = isDarkMode ? Colors.Black : Colors.White,
            };
            menuContainer.AddThemeStyleboxOverride("panel", box);
            menuBar.AddThemeColorOverride("font_color", isDarkMode ? Colors.White : Colors.Black);
            menuBar.AddThemeColorOverride("font_pressed_color", isDarkMode ? Colors.White : Colors.Black);
            menuBar.AddThemeColorOverride("font_hover_color", isDarkMode ? Colors.White : Colors.Black);
            menuBar.AddThemeColorOverride("font_hover_pressed_color", isDarkMode ? Colors.White : Colors.Black);
            menuBar.AddThemeColorOverride("font_focus_color", isDarkMode ? Colors.White : Colors.Black);
        }

        public override void _ExitTree()
        {
            GetWindow().FilesDropped -= DroppedFiles;
            fileMenu.IdPressed -= FileMenuPressed;
            assetsMenu.IdPressed -= AssetsMenuPressed;
            openFile.DirSelected -= OpenDirSelected;
            openFile.FilesSelected -= OpenFileSelected;
        }

        public override void _Process(double delta)
        {
        }

        public void RefreshAssets()
        {
            assetsTree.Clear();
            var root = assetsTree.CreateItem();
            if (string.IsNullOrEmpty(workingDirectory))
            {
                root.SetText(0, "No project open.");
                return;
            }
            root.SetText(0, workingDirectory.GetFile());
            AddFolder(workingDirectory, assetsTree, root);
        }

        public void SaveProject(string folder)
        {
        }

        public void SaveScene()
        {
        }

        public void ExportProject(string folder)
        {
        }

        private void AddFolder(string folder, Tree tree, TreeItem item)
        {
            string[] dirs = DirAccess.GetDirectoriesAt(folder);
            string[] files = DirAccess.GetFilesAt(folder);
            foreach (var dir in dirs)
            {
                var dirItem = tree.CreateItem(item);
                dirItem.SetText(0, dir);
                dirItem.SetMetadata(0, folder.PathJoin(dir));
                AddFolder(folder.PathJoin(dir), tree, dirItem);
            }
            foreach (var file in files)
            {
                var fileItem = tree.CreateItem(item);
                fileItem.SetText(0, file);
                fileItem.SetMetadata(0, folder.PathJoin(file));
            }
        }

        private void FileMenuPressed(long id)
        {
            switch (id)
            {
                case 0: // open
                    openFile.FileMode = FileDialog.FileModeEnum.OpenDir;
                    openFile.ClearFilters();
                    openFile.Popup();
                    break;
                case 3: // close
                    OpenDirSelected(string.Empty);
                    break;
                case 1: // save
                case 2: // save as
                    SaveProject(workingDirectory);
                    break;
                case 4: // export
                    ExportProject(workingDirectory);
                    break;
            }
        }

        private void AssetsMenuPressed(long id)
        {
            switch (id)
            {
                case 0: // import
                    openFile.FileMode = FileDialog.FileModeEnum.OpenFiles;
                    openFile.ClearFilters();
                    openFile.Popup();
                    break;
                case 1: // save
                    SaveScene();
                    break;
            }
        }

        private void OpenDirSelected(string paths)
        {
            workingDirectory = paths;
            RefreshAssets();
        }

        private void OpenFileSelected(string[] paths)
        {
        }

        private void DroppedFiles(string[] files)
        {
        }
    }
}