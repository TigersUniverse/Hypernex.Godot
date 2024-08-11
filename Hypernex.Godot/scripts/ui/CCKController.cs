using System;
using System.Collections.Generic;
using Godot;
using Hypernex.Tools;
using HypernexSharp.APIObjects;

namespace Hypernex.UI
{
    public partial class CCKController : Node
    {
        [Export]
        public RichTextLabel worldLabel;
        [Export]
        public OptionButton worldOptions;
        [Export]
        public RichTextLabel worldMetaLabel;
        [Export]
        public AcceptDialog dialog;
        [Export]
        public LineEdit worldNameEdit;
        [Export]
        public TextEdit worldDescriptionEdit;

        public string selectedWorldPath;
        public WorldMeta selectedWorldMeta;
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
                worldIds = APITools.CurrentUser.Worlds.ToArray();
            else
                worldIds = Array.Empty<string>();
            worldOptions.Disabled = true;
            worldOptions.Clear();
            worldOptions.AddItem("New World", 0);
            worldOptions.Selected = 0;
            int found = 0;
            for (int i = 0; i < worldIds.Length; i++)
            {
                int j = i;
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
            }
            WorldMetaSelected(0);
        }

        public void WorldMetaSelected(int index)
        {
            worldNameEdit.Clear();
            worldDescriptionEdit.Clear();
            if (worldOptions.GetItemId(index) == 0)
            {
                worldMetaLabel.Text = "No world selected, a new one will be created upon upload.";
                selectedWorldMeta = new WorldMeta(string.Empty, APITools.CurrentUser?.Id ?? string.Empty, WorldPublicity.OwnerOnly, string.Empty, string.Empty, string.Empty);
                return;
            }
            string id = worldIds[worldOptions.GetItemId(index) - 1];
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
            });
        }

        public void ClearSelectedWorld()
        {
            selectedWorldPath = string.Empty;
            worldLabel.Text = "No world file selected.";
        }

        public void WorldSelected(string path)
        {
            selectedWorldPath = path;
            worldLabel.Text = $"Selected world file:\n[code]{path.Replace("[", "[lb]")}[/code]";
        }

        public void UploadSelectedWorld()
        {
            if (!string.IsNullOrWhiteSpace(worldNameEdit.Text))
                selectedWorldMeta.Name = worldNameEdit.Text;
            if (!string.IsNullOrWhiteSpace(worldDescriptionEdit.Text))
                selectedWorldMeta.Description = worldDescriptionEdit.Text;
            APITools.UploadWorld(selectedWorldPath, selectedWorldMeta, (success, msg) =>
            {
                if (success)
                {
                    dialog.Title = "World Uploaded";
                }
                else
                {
                    dialog.Title = "Upload Failed";
                }
                dialog.DialogText = msg;
                dialog.Show();
            });
        }

        public void LoadSelectedWorld()
        {
            if (string.IsNullOrWhiteSpace(selectedWorldPath))
            {
                dialog.Title = "World not found";
                dialog.DialogText = "Please select a world file.";
                dialog.Show();
                return;
            }
            GameInstance inst = GameInstance.FromLocalFile(selectedWorldPath);
            if (inst.IsDisposed)
            {
                dialog.Title = "World not found";
                dialog.DialogText = "Please select a world file.";
                dialog.Show();
            }
        }
    }
}