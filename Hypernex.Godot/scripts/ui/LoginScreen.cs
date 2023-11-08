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
        public event EventHandler<(User, Token)> OnUser;

        public override void _Ready()
        {
            loginButton.Pressed += TryLogin;
        }

        public override void _ExitTree()
        {
            loginButton.Pressed -= TryLogin;
        }

        private void OnLogin(User user, Token token)
        {
            OnUser?.Invoke(this, (user, token));
        }

        public void TryLoginWith()
        {
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
            HypernexSettings settings = new HypernexSettings(usernameEdit.Text, passwordEdit.Text, twoFactorEdit.Text)
            {
                TargetDomain = "play.hypernex.dev",
                IsHTTP = false,
            };
            TryLogin(settings);
        }

        public void TryLogin(HypernexSettings settings)
        {
            Init.Instance.hypernex = new HypernexObject(settings);
            loginButton.Disabled = true;
            Init.Instance.hypernex.Login(r =>
            {
                QuickInvoke.InvokeActionOnMainThread(() =>
                {
                    if (r.success)
                    {
                        if (r.result.Result == LoginResult.Correct)
                        {
                            Init.Instance.hypernex.GetUser(r.result.Token, userR =>
                            {
                                QuickInvoke.InvokeActionOnMainThread(() =>
                                {
                                    if (userR.success)
                                    {
                                        bool found = false;
                                        foreach (var user in ConfigManager.LoadedConfig.SavedAccounts)
                                        {
                                            if (user.UserId == userR.result.UserData.Id)
                                            {
                                                user.TokenContent = r.result.Token.content;
                                                found = true;
                                                break;
                                            }
                                        }
                                        if (!found)
                                        {
                                            ConfigManager.LoadedConfig.SavedAccounts.Add(new ConfigUser()
                                            {
                                                UserId = userR.result.UserData.Id,
                                                Username = userR.result.UserData.Username,
                                                TokenContent = r.result.Token.content,
                                                Server = settings.TargetDomain,
                                            });
                                        }
                                        messagePopup.DialogText = $"Welcome {userR.result.UserData.Username}!";
                                        messagePopup.Show();
                                        loginButton.Disabled = false;
                                        OnLogin(userR.result.UserData, r.result.Token);
                                    }
                                    else
                                    {
                                        messagePopup.DialogText = "Failed to get login user!";
                                        messagePopup.Show();
                                        loginButton.Disabled = false;
                                    }
                                });
                            });
                        }
                        else
                        {
                            messagePopup.DialogText = $"Error logging in: {r.result.Result}";
                            messagePopup.Show();
                            loginButton.Disabled = false;
                        }
                    }
                    else
                    {
                        messagePopup.DialogText = $"API error: {r.message}";
                        messagePopup.Show();
                        loginButton.Disabled = false;
                    }
                });
            });
        }
    }
}