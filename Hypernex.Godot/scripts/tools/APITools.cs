using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK;
using Hypernex.Configuration;
using Hypernex.Configuration.ConfigMeta;
using HypernexSharp;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing;

namespace Hypernex.Tools
{
    public static class APITools
    {
        public static HypernexObject APIObject { get; internal set; }
        public static HypernexSettings APISettings { get; internal set; }
        public static User CurrentUser { get; internal set; }
        public static UserSocket UserSocket { get; internal set; }
        public static Action OnSocketConnect { get; set; } = () => { };
        public static Action<User> OnUserRefresh { get; set; } = _ => { };
        public static Action OnLogout { get; set; } = () => { };
        public static bool IsFullReady => APIObject != null && CurrentUser != null && CurrentToken != null && (UserSocket?.IsOpen ?? false);

        internal static Token CurrentToken { get; set; }
        private static List<WorldMeta> CachedWorldMeta = new();

        public static void Create(HypernexSettings settings)
        {
            APISettings = settings;
            APIObject = new HypernexObject(APISettings);
        }

        public static void Login(Action<bool, string> threadedCallback = null)
        {
            APIObject.Login(r =>
            {
                if (r.success)
                {
                    if (r.result.Result == LoginResult.Correct)
                    {
                        APIObject.GetUser(r.result.Token, userR =>
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
                                        Server = APISettings.TargetDomain,
                                    });
                                }
                                OnLogin(userR.result.UserData, r.result.Token);
                                threadedCallback?.Invoke(true, $"Welcome {userR.result.UserData.Username}!");
                            }
                            else
                            {
                                threadedCallback?.Invoke(false, "Failed to get login user!");
                            }
                        });
                    }
                    else
                    {
                        threadedCallback?.Invoke(false, $"Error logging in: {r.result.Result}");
                    }
                }
                else
                {
                    threadedCallback?.Invoke(false, $"API error: {r.message}");
                }
            });
        }

        private static void OnLogin(User user, Token token)
        {
            CurrentUser = user;
            CurrentToken = token;
            QuickInvoke.InvokeActionOnMainThread(OnUserRefresh, CurrentUser);
        }

        public static void Logout()
        {
            OnLogout.Invoke();
            // APIObject.Logout(r => QuickInvoke.InvokeActionOnMainThread(OnLogout), CurrentUser, CurrentToken);
        }

        public static void CreateUserSocket(Action callback)
        {
            UserSocket = APIObject.OpenUserSocket(CurrentUser, CurrentToken, () => QuickInvoke.InvokeActionOnMainThread(callback), false);
        }

        public static void DisposeUserSocket()
        {
            APIObject.CloseUserSocket();
            UserSocket = null;
        }

        public static string GetUsersName(this User user)
        {
            return string.IsNullOrEmpty(user?.Bio?.DisplayName) ? user?.Username : user?.Bio?.DisplayName;
        }

        public static void GetWorldMeta(string worldId, Action<WorldMeta> callback)
        {
            if (CachedWorldMeta.Any(x => x.Id == worldId))
            {
                WorldMeta worldMeta = CachedWorldMeta.First(x => x.Id == worldId);
                Logger.CurrentLogger.Debug(worldMeta.Id);
                callback.Invoke(worldMeta);
                return;
            }
            APIObject.GetWorldMeta(result =>
            {
                if (result.success)
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    {
                        CachedWorldMeta.Add(result.result.Meta);
                        callback.Invoke(result.result.Meta);
                    }));
                else
                    QuickInvoke.InvokeActionOnMainThread(callback, null);
            }, worldId);
        }
    }
}
