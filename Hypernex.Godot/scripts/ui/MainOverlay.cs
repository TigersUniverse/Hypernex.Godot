using System;
using System.Linq;
using Godot;
using Hypernex.Tools;

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

        public override void _Ready()
        {
            logoutButton.Pressed += APITools.Logout;
            exitButton.Pressed += Exit;
        }

        public override void _ExitTree()
        {
            logoutButton.Pressed -= APITools.Logout;
            exitButton.Pressed -= Exit;
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