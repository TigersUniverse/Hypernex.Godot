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
        public LineEdit usernameEdit;
        [Export]
        public LineEdit passwordEdit;
        [Export]
        public LineEdit twoFactorEdit;
        [Export]
        public Button loginButton;
        [Export]
        public AcceptDialog messagePopup;
        public HypernexObject hypernex;

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
            if (ConfigManager.LoadedConfig.SavedAccounts.Any())
            {
                ConfigUser user = ConfigManager.LoadedConfig.SavedAccounts.First();
                HypernexSettings settings = new HypernexSettings(user.UserId, user.TokenContent)
                {
                    TargetDomain = "play.hypernex.dev",
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
            hypernex = new HypernexObject(settings);
            hypernex.Login(r =>
            {
                QuickInvoke.InvokeActionOnMainThread(() =>
                {
                    if (r.success)
                    {
                        if (r.result.Result == LoginResult.Correct)
                        {
                            hypernex.GetUser(r.result.Token, userR =>
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
                                            });
                                        }
                                        messagePopup.DialogText = $"Welcome back {userR.result.UserData.Username}!";
                                        messagePopup.Show();
                                    }
                                    else
                                    {
                                        messagePopup.DialogText = "Failed to get login user!";
                                        messagePopup.Show();
                                    }
                                });
                            });
                        }
                        else
                        {
                            messagePopup.DialogText = $"Error logging in: {r.result.Result}";
                            messagePopup.Show();
                        }
                    }
                    else
                    {
                        messagePopup.DialogText = $"API error: {r.message}";
                        messagePopup.Show();
                    }
                });
            });
        }
    }
}