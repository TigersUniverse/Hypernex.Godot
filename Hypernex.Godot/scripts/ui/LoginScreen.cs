using System;
using System.Linq;
using Godot;
using Hypernex.Configuration;
using Hypernex.Configuration.ConfigMeta;
using Hypernex.Tools;
using HypernexSharp;

namespace Hypernex.UI
{
    public partial class LoginScreen : Node
    {
        [Export]
        public VBoxContainer root;
        [Export]
        public LineEdit usernameEdit;
        [Export]
        public LineEdit passwordEdit;
        [Export]
        public LineEdit twoFactorEdit;
        [Export]
        public Button loginButton;
        [Export]
        public AcceptDialog messagePopup;

        public override void _Ready()
        {
            loginButton.Pressed += TryLogin;
        }

        public override void _ExitTree()
        {
            loginButton.Pressed -= TryLogin;
        }

        public void TryLoginWith()
        {
            return;
            if (ConfigManager.LoadedConfig.SavedAccounts.Any())
            {
                ConfigUser user = ConfigManager.LoadedConfig.SavedAccounts.First();
                HypernexSettings settings = new HypernexSettings(user.UserId, user.TokenContent)
                {
                    TargetDomain = user.Server,
                    IsHTTP = false,
                };
                TryLogin(settings);
            }
        }

        private void TryLogin()
        {
            if (ConfigManager.LoadedConfig.SavedAccounts.Any(x => x.Username == usernameEdit.Text) && string.IsNullOrEmpty(passwordEdit.Text))
            {
                ConfigUser user = ConfigManager.LoadedConfig.SavedAccounts.First(x => x.Username == usernameEdit.Text);
                HypernexSettings settings = new HypernexSettings(user.UserId, user.TokenContent)
                {
                    TargetDomain = user.Server,
                    IsHTTP = false,
                };
                TryLogin(settings);
            }
            else
            {
                HypernexSettings settings = new HypernexSettings(usernameEdit.Text, passwordEdit.Text, twoFactorEdit.Text)
                {
                    TargetDomain = "play.hypernex.dev",
                    IsHTTP = false,
                };
                TryLogin(settings);
            }
        }

        public void TryLogin(HypernexSettings settings)
        {
            loginButton.Disabled = true;
            APITools.Create(settings);
            APITools.Login((s, m) =>
            {
                QuickInvoke.InvokeActionOnMainThread(() =>
                {
                    messagePopup.DialogText = m;
                    messagePopup.Show();
                    loginButton.Disabled = false;
                });
            });
        }
    }
}