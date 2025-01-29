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
        public Control root;
        [Export]
        public LineEdit serverAddressEdit;
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
        [Export]
        public bool useHttp = false;
        [Export]
        public Button exitButton;

        public override void _EnterTree()
        {
            LoadedConfig(ConfigManager.LoadedConfig);
            loginButton.Pressed += TryLogin;
            // serverAddressEdit.TextSubmitted += Submit;
            usernameEdit.TextSubmitted += Submit;
            passwordEdit.TextSubmitted += Submit;
            loginOptions.ItemSelected += AccountSelected;
            ConfigManager.OnConfigLoaded += LoadedConfig;
            exitButton.Pressed += Exit;
        }

        public override void _ExitTree()
        {
            loginButton.Pressed -= TryLogin;
            // serverAddressEdit.TextSubmitted -= Submit;
            usernameEdit.TextSubmitted -= Submit;
            passwordEdit.TextSubmitted -= Submit;
            loginOptions.ItemSelected -= AccountSelected;
            ConfigManager.OnConfigLoaded -= LoadedConfig;
            exitButton.Pressed -= Exit;
        }

        private void LoadedConfig(Config config)
        {
            loginOptions.Clear();
            loginOptions.AddItem("Select Account");
            if (config == null)
                return;
            foreach (var acct in config.SavedAccounts)
            {
                loginOptions.AddItem($"{acct.Username} - {acct.Server}");
            }
        }

        private void AccountSelected(long index)
        {
            if (index == 0)
            {
                LoginNoUser();
                return;
            }
            ConfigUser user = ConfigManager.LoadedConfig.SavedAccounts[(int)index - 1];
            HypernexSettings settings = new HypernexSettings(user.UserId, user.TokenContent)
            {
                TargetDomain = user.Server,
                IsHTTP = useHttp,
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
                    IsHTTP = useHttp,
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
                    IsHTTP = useHttp,
                };
                TryLogin(settings);
            }
            else
            {
                string addr = string.IsNullOrWhiteSpace(serverAddressEdit.Text) ? "play.hypernex.dev" : serverAddressEdit.Text;
                HypernexSettings settings = new HypernexSettings(usernameEdit.Text, passwordEdit.Text, twoFactorEdit.Text)
                {
                    TargetDomain = addr,
                    IsHTTP = useHttp,
                };
                TryLogin(settings);
            }
        }

        public void TryLogin(HypernexSettings settings)
        {
            // loginButton.Disabled = true;
            loginOptions.Selected = 0;
            APITools.Create(settings);
            APITools.Login((s, m) =>
            {
                if (s)
                    return;
                QuickInvoke.InvokeActionOnMainThread(() =>
                {
                    messagePopup.DialogText = m;
                    messagePopup.Show();
                    loginButton.Disabled = false;
                    passwordEdit.Clear();
                    twoFactorEdit.Clear();
                });
            });
        }

        public void LoginNoUser()
        {
            Init.Instance.OnLogin(null);
        }

        public void Exit()
        {
            GetTree().Quit();
        }
    }
}