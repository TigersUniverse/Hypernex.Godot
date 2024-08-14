using System;
using System.Collections.Generic;
using Godot;
using Hypernex.Tools;
using HypernexSharp.APIObjects;

namespace Hypernex.UI
{
    public partial class CCKUploadController : Node
    {
        public enum CCKFileType
        {
            World = 0,
            Avatar = 1,
        }

        [Export]
        public CCKController cck;
        [Export]
        public CCKFileType fileType = CCKFileType.World;
        [Export]
        public RichTextLabel worldLabel;
        [Export]
        public OptionButton worldOptions;
        [Export]
        public RichTextLabel worldMetaLabel;
        [Export]
        public LineEdit worldNameEdit;
        [Export]
        public TextEdit worldDescriptionEdit;

        public string selectedWorldPath;
        public WorldMeta selectedWorldMeta;
        public AvatarMeta selectedAvatarMeta;
        public string[] worldIds;
        private bool worldsLoading;

        public override void _EnterTree()
        {
            Reset();
        }

        public void Reset()
        {
            ClearSelectedWorld();
            RefreshWorlds();
        }

        public void RefreshWorlds()
        {
            worldsLoading = true;
            if (APITools.IsFullReady)
            {
                switch (fileType)
                {
                    case CCKFileType.World:
                        worldIds = APITools.CurrentUser.Worlds.ToArray();
                        break;
                    case CCKFileType.Avatar:
                        worldIds = APITools.CurrentUser.Avatars.ToArray();
                        break;
                }
            }
            else
                worldIds = Array.Empty<string>();
            worldOptions.Disabled = true;
            worldOptions.Clear();
            worldOptions.AddItem($"New {fileType}", 0);
            worldOptions.Selected = 0;
            int found = 0;
            for (int i = 0; i < worldIds.Length; i++)
            {
                int j = i;
                switch (fileType)
                {
                    case CCKFileType.World:
                        APITools.GetWorldMeta(worldIds[j], meta =>
                        {
                            if (meta == null)
                                worldOptions.AddItem(worldIds[j], j+1);
                            else
                                worldOptions.AddItem(meta.Name, j+1);
                            found++;
                            if (found >= worldIds.Length)
                            {
                                worldsLoading = false;
                                worldOptions.Disabled = false;
                            }
                        });
                        break;
                    case CCKFileType.Avatar:
                        APITools.GetAvatarMeta(worldIds[j], meta =>
                        {
                            if (meta == null)
                                worldOptions.AddItem(worldIds[j], j+1);
                            else
                                worldOptions.AddItem(meta.Name, j+1);
                            found++;
                            if (found >= worldIds.Length)
                            {
                                worldsLoading = false;
                                worldOptions.Disabled = false;
                            }
                        });
                        break;
                }
            }
            WorldMetaSelected(0);
        }

        public void WorldMetaSelected(int index)
        {
            worldNameEdit.Clear();
            worldDescriptionEdit.Clear();
            if (worldOptions.GetItemId(index) == 0)
            {
                worldMetaLabel.Text = $"No {fileType} selected, a new one will be created upon upload.";
                selectedWorldMeta = new WorldMeta(string.Empty, APITools.CurrentUser?.Id ?? string.Empty, WorldPublicity.OwnerOnly, string.Empty, string.Empty, string.Empty);
                selectedAvatarMeta = new AvatarMeta(string.Empty, APITools.CurrentUser?.Id ?? string.Empty, AvatarPublicity.OwnerOnly, string.Empty, string.Empty, string.Empty);
                return;
            }
            string id = worldIds[worldOptions.GetItemId(index) - 1];
            switch (fileType)
            {
                case CCKFileType.World:
                    APITools.GetWorldMeta(id, meta =>
                    {
                        if (meta == null)
                            worldMetaLabel.Text = $"Existing world id selected: {id.Replace("[", "[lb]")}";
                        else
                        {
                            worldNameEdit.Text = meta.Name;
                            worldDescriptionEdit.Text = meta.Description;
                            worldMetaLabel.Text = $"Existing world selected: {meta.Name.Replace("[", "[lb]")}";
                        }
                        selectedWorldMeta = meta;
                        selectedAvatarMeta = null;
                    });
                    break;
                case CCKFileType.Avatar:
                    APITools.GetAvatarMeta(id, meta =>
                    {
                        if (meta == null)
                            worldMetaLabel.Text = $"Existing avatar id selected: {id.Replace("[", "[lb]")}";
                        else
                        {
                            worldNameEdit.Text = meta.Name;
                            worldDescriptionEdit.Text = meta.Description;
                            worldMetaLabel.Text = $"Existing avatar selected: {meta.Name.Replace("[", "[lb]")}";
                        }
                        selectedAvatarMeta = meta;
                        selectedWorldMeta = null;
                    });
                    break;
            }
        }

        public void ClearSelectedWorld()
        {
            selectedWorldPath = string.Empty;
            worldLabel.Text = $"No {fileType} file selected.";
        }

        public void WorldSelected(string path)
        {
            selectedWorldPath = path;
            worldLabel.Text = $"Selected {fileType} file:\n[code]{path.Replace("[", "[lb]")}[/code]";
        }

        public void UploadSelectedWorld()
        {
            if (!string.IsNullOrWhiteSpace(worldNameEdit.Text))
                selectedWorldMeta.Name = worldNameEdit.Text;
            if (!string.IsNullOrWhiteSpace(worldDescriptionEdit.Text))
                selectedWorldMeta.Description = worldDescriptionEdit.Text;
            switch (fileType)
            {
                case CCKFileType.World:
                    APITools.UploadWorld(selectedWorldPath, selectedWorldMeta, (success, msg) =>
                    {
                        string title;
                        if (success)
                        {
                            title = "World Uploaded";
                        }
                        else
                        {
                            title = "Upload Failed";
                        }
                        cck.Popup(title, msg);
                    });
                    break;
                case CCKFileType.Avatar:
                    APITools.UploadAvatar(selectedWorldPath, selectedAvatarMeta, (success, msg) =>
                    {
                        string title;
                        if (success)
                        {
                            title = "Avatar Uploaded";
                        }
                        else
                        {
                            title = "Upload Failed";
                        }
                        cck.Popup(title, msg);
                    });
                    break;
            }
        }

        public void LoadSelectedWorld()
        {
            if (fileType != CCKFileType.World)
                return;
            if (string.IsNullOrWhiteSpace(selectedWorldPath))
            {
                cck.Popup($"{fileType} not found", $"Please select a {fileType} file");
                return;
            }
            GameInstance inst = GameInstance.FromLocalFile(selectedWorldPath);
            if (inst.IsDisposed)
            {
                cck.Popup($"{fileType} not valid", $"Please select a valid {fileType} file");
            }
        }
    }
}