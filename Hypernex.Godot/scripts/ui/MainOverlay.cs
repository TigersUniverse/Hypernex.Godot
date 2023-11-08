using System;
using System.Linq;
using Godot;
using Hypernex.CCK;
using Hypernex.Configuration;
using Hypernex.Configuration.ConfigMeta;
using Hypernex.Tools;
using HypernexSharp;
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
        public event EventHandler OnLogout;

        public override void _Ready()
        {
            logoutButton.Pressed += Logout;
            exitButton.Pressed += Exit;
        }

        public override void _ExitTree()
        {
            logoutButton.Pressed -= Logout;
            exitButton.Pressed -= Exit;
        }

        private void Exit()
        {
            GetTree().Quit();
        }

        private void Logout()
        {
            Init.Instance.hypernex.Logout(r =>
            {
                QuickInvoke.InvokeActionOnMainThread(() =>
                {
                    OnLogout?.Invoke(this, EventArgs.Empty);
                });
            }, Init.Instance.user, Init.Instance.token);
        }

        public void ShowHome()
        {
            tabs.CurrentTab = 0;
        }
    }
}