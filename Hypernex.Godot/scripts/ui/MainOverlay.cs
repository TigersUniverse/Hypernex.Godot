using System;
using System.Linq;
using Godot;
using Hypernex.Tools;
using HypernexSharp.APIObjects;

namespace Hypernex.UI
{
    public partial class MainOverlay : Node
    {
        [Export]
        public Control root;
        [Export]
        public TabContainer tabs;
        [Export]
        public Button logoutButton;
        [Export]
        public Button exitButton;
        [Export]
        public int currentInstanceIdx;

        public override void _Ready()
        {
            logoutButton.Pressed += APITools.Logout;
            exitButton.Pressed += Exit;
            GameInstance.OnGameInstanceLoaded += GameInstanceLoaded;
            tabs?.SetTabHidden(currentInstanceIdx, true);
        }

        public override void _ExitTree()
        {
            logoutButton.Pressed -= APITools.Logout;
            exitButton.Pressed -= Exit;
            GameInstance.OnGameInstanceLoaded -= GameInstanceLoaded;
        }

        private void GameInstanceLoaded(GameInstance instance, WorldMeta meta)
        {
            tabs?.SetTabHidden(currentInstanceIdx, (instance?.IsDisposed) ?? true);
            if (tabs.GetTabControl(currentInstanceIdx) is BigCardTemplate card)
            {
                card.SetGameInstance(instance);
            }
            if ((instance?.IsDisposed ?? true) && tabs.CurrentTab == currentInstanceIdx)
            {
                tabs.CurrentTab = 0;
            }
        }

        private void Exit()
        {
            GetTree().Quit();
        }

        public void ShowHome()
        {
            tabs.CurrentTab = 0;
        }
    }
}