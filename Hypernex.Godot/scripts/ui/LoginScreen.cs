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
        public OptionButton loginOptions;
        [Export]
        public AcceptDialog messagePopup;

        public override void _Ready()
        {
            LoadedConfig(ConfigManager.LoadedConfig);
            loginButton.Pressed += TryLogin;
            usernameEdit.TextSubmitted += Submit;
            loginOptions.ItemSelected += AccountSelected;
            ConfigManager.OnConfigLoaded += LoadedConfig;
        }

        public override void _ExitTree()
        {
            loginButton.Pressed -= TryLogin;
            usernameEdit.TextSubmitted -= Submit;
            loginOptions.ItemSelected -= AccountSelected;
            ConfigManager.OnConfigLoaded -= LoadedConfig;
        }

        private void LoadedConfig(Config config)
        {
            loginOptions.Clear();
            loginOptions.AddItem("Select Account");
            if (config == null)
                return;
            foreach (var acct in config.SavedAccounts)
            {
                loginOptions.AddItem(acct.Username);
            }
        }

        private void AccountSelected(long index)
        {
            ConfigUser user = ConfigManager.LoadedConfig.SavedAccounts[(int)index - 1];
            HypernexSettings settings = new HypernexSettings(user.UserId, user.TokenContent)
            {
                TargetDomain = user.Server,
                IsHTTP = true,
            };
            TryLogin(settings);
        }

        private void Submit(string newText)
        {
            if (loginButton.Disabled)
                return;
            TryLogin();
        }

        public void TryLoginWith()
        {
            if (ConfigManager.LoadedConfig.SavedAccounts.Any())
            {
                ConfigUser user = ConfigManager.LoadedConfig.SavedAccounts.First();
                HypernexSettings settings = new HypernexSettings(user.UserId, user.TokenContent)
                {
                    TargetDomain = user.Server,
                    IsHTTP = true,
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
                    IsHTTP = true,
                };
                TryLogin(settings);
            }
            else
            {
                HypernexSettings settings = new HypernexSettings(usernameEdit.Text, passwordEdit.Text, twoFactorEdit.Text)
                {
                    TargetDomain = "192.168.1.10",
                    IsHTTP = true,
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