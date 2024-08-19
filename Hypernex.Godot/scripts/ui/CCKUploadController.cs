using System;
using System.Collections.Generic;
using Godot;
using Hypernex.CCK;
using Hypernex.Configuration;
using Hypernex.Game;
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
        public RichTextLabel scriptsLabel;
        [Export]
        public OptionButton worldOptions;
        [Export]
        public RichTextLabel worldMetaLabel;
        [Export]
        public LineEdit worldNameEdit;
        [Export]
        public TextEdit worldDescriptionEdit;

        public string selectedWorldPath;
        public string[] selectedScriptPaths = Array.Empty<string>();
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
            ClearSelectedScripts();
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
                        selectedAvatarMeta = new AvatarMeta(string.Empty, APITools.CurrentUser?.Id ?? string.Empty, AvatarPublicity.OwnerOnly, string.Empty, string.Empty, string.Empty);
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
                        selectedWorldMeta = new WorldMeta(string.Empty, APITools.CurrentUser?.Id ?? string.Empty, WorldPublicity.OwnerOnly, string.Empty, string.Empty, string.Empty);
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

        public void WorldServerScripts(string[] paths)
        {
            selectedScriptPaths = paths;
            if (IsInstanceValid(scriptsLabel))
                scriptsLabel.Text = $"Selected scripts:\n[code]{string.Join("\n", selectedScriptPaths)}[/code]";
        }

        public void ClearSelectedScripts()
        {
            selectedScriptPaths = Array.Empty<string>();
            if (IsInstanceValid(scriptsLabel))
                scriptsLabel.Text = "No server scripts selected";
        }

        public void UploadSelectedWorld()
        {
            // selectedWorldMeta.Publicity = WorldPublicity.Anyone;
            // selectedAvatarMeta.Publicity = AvatarPublicity.Anyone;

            if (!string.IsNullOrWhiteSpace(worldNameEdit.Text))
                selectedWorldMeta.Name = worldNameEdit.Text;
            if (!string.IsNullOrWhiteSpace(worldDescriptionEdit.Text))
                selectedWorldMeta.Description = worldDescriptionEdit.Text;
            if (!string.IsNullOrWhiteSpace(worldNameEdit.Text))
                selectedAvatarMeta.Name = worldNameEdit.Text;
            if (!string.IsNullOrWhiteSpace(worldDescriptionEdit.Text))
                selectedAvatarMeta.Description = worldDescriptionEdit.Text;
            switch (fileType)
            {
                case CCKFileType.World:
                    List<NexboxScript> serverScripts = new List<NexboxScript>();
                    foreach (var script in selectedScriptPaths)
                    {
                        serverScripts.Add(new NexboxScript(script.GetExtension() == "js" ? NexboxLanguage.JavaScript : NexboxLanguage.Lua, FileAccess.GetFileAsString(script))
                        {
                            Name = System.IO.Path.GetFileNameWithoutExtension(script),
                        });
                    }
                    APITools.UploadScripts(serverScripts, scripts => {
                        APITools.UploadWorld(selectedWorldPath, selectedWorldMeta, scripts, (success, msg) =>
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
            switch (fileType)
            {
                case CCKFileType.World:
                {
                    if (string.IsNullOrWhiteSpace(selectedWorldPath))
                    {
                        cck.Popup("World not found", "Please select a world file");
                        break;
                    }
                    GameInstance inst = GameInstance.FromLocalFile(selectedWorldPath);
                    if (inst.IsDisposed)
                    {
                        cck.Popup("World not valid", "Please select a valid world file");
                    }
                    break;
                }
                case CCKFileType.Avatar:
                {
                    if (string.IsNullOrEmpty(selectedAvatarMeta.Id))
                    {
                        cck.Popup("Avatar not found", "Please select an already uploaded avatar");
                        break;
                    }
                    ConfigManager.SelectedConfigUser.CurrentAvatar = selectedAvatarMeta.Id;
                    ConfigManager.SaveConfigToFile();
                    if (IsInstanceValid(PlayerRoot.Local))
                    {
                        PlayerRoot.Local.ChangeAvatar(selectedAvatarMeta.Id);
                    }
                    break;
                }
            }
        }
    }
}