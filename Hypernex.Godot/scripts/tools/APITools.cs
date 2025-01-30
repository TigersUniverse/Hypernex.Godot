using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
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
        public static Action<User> OnUserLogin { get; set; } = _ => { };
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
            QuickInvoke.InvokeActionOnMainThread(OnUserLogin, CurrentUser);
            CachedWorldMeta.Clear();
        }

        public static void Logout()
        {
            OnLogout.Invoke();
            // APIObject.Logout(r => QuickInvoke.InvokeActionOnMainThread(OnLogout), CurrentUser, CurrentToken);
            CachedWorldMeta.Clear();
        }

        public static void CreateUserSocket(Action callback)
        {
            UserSocket = APIObject.OpenUserSocket(CurrentUser, CurrentToken, () => QuickInvoke.InvokeActionOnMainThread(callback), false);
        }

        public static void DisposeUserSocket()
        {
            APIObject?.CloseUserSocket();
            UserSocket = null;
        }

        public static string GetUsersName(this User user)
        {
            return string.IsNullOrEmpty(user?.Bio?.DisplayName) ? user?.Username : user?.Bio?.DisplayName;
        }

        public static void RefreshUser(Action callback)
        {
            CachedWorldMeta.Clear();
            APIObject.GetUser(CurrentToken, r =>
            {
                if (r.success)
                {
                    QuickInvoke.InvokeActionOnMainThread(() =>
                    {
                        CurrentUser = r.result.UserData;
                    });
                    QuickInvoke.InvokeActionOnMainThread(callback);
                }
            });
        }

        public static void GetAvatarMeta(string avatarId, Action<AvatarMeta> callback)
        {
            APIObject.GetAvatarMeta(result =>
            {
                if (result.success)
                    QuickInvoke.InvokeActionOnMainThread(callback, result.result.Meta);
                else
                    QuickInvoke.InvokeActionOnMainThread(callback, (AvatarMeta)null);
            }, avatarId);
        }

        public static void GetWorldMeta(string worldId, Action<WorldMeta> callback)
        {
            /*
            if (CachedWorldMeta.Any(x => x.Id == worldId))
            {
                WorldMeta worldMeta = CachedWorldMeta.First(x => x.Id == worldId);
                Logger.CurrentLogger.Debug(worldMeta.Id);
                callback.Invoke(worldMeta);
                return;
            }
            */
            APIObject.GetWorldMeta(result =>
            {
                if (result.success)
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    {
                        CachedWorldMeta.Add(result.result.Meta);
                        callback.Invoke(result.result.Meta);
                    }));
                else
                    QuickInvoke.InvokeActionOnMainThread(callback, (WorldMeta)null);
            }, worldId);
        }

        public static void UploadAvatar(string fullPath, AvatarMeta meta, Action<bool, string> callback = null)
        {
            AvatarMeta metaFinal = new AvatarMeta(meta.Id, meta.OwnerId, meta.Publicity, meta.Name, meta.Description, meta.ImageURL);
            try
            {
                FileStream fs = new FileStream(fullPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Delete | FileShare.Read);
                if (fs.Length > 1048576 * 90)
                {
                    string tempDir = Path.Combine(OS.GetUserDataDir(), "file_parts");
                    Directory.CreateDirectory(tempDir);
                    APIObject.UploadPart(result =>
                    {
                        fs.Dispose();
                        Directory.Delete(tempDir, true);
                        if (result.success)
                            Logger.CurrentLogger.Log("Uploaded avatar!");
                        else
                            Logger.CurrentLogger.Error($"Failed to upload avatar! {result.message}");
                        if (callback != null)
                            QuickInvoke.InvokeActionOnMainThread(callback, result.success, result.message);
                    }, CurrentUser, CurrentToken, fs, tempDir, avatarMeta: metaFinal);
                }
                else
                {
                    APIObject.UploadAvatar(result =>
                    {
                        fs.Dispose();
                        if (result.success)
                            Logger.CurrentLogger.Log("Uploaded avatar!");
                        else
                            Logger.CurrentLogger.Error($"Failed to upload avatar! {result.message}");
                        if (callback != null)
                            QuickInvoke.InvokeActionOnMainThread(callback, result.success, result.message);
                    }, CurrentUser, CurrentToken, fs, metaFinal);
                }
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error(e);
                callback?.Invoke(false, e.Message);
            }
        }

        public static void UploadScripts(List<NexboxScript> serverScripts, Action<List<string>> callback = null, List<string> a = null, List<NexboxScript> b = null)
        {
            List<string> scripts;
            if (a == null)
                scripts = new List<string>();
            else
                scripts = a;
            List<NexboxScript> ss;
            if (b == null)
                ss = new List<NexboxScript>(serverScripts);
            else
                ss = b;
            if (ss.Count <= 0)
            {
                callback?.Invoke(scripts);
                // QuickInvoke.InvokeActionOnMainThread(callback, scripts);
                return;
            }
            NexboxScript script = ss[0];
            string tempDir2 = Path.Combine(OS.GetUserDataDir(), "file_parts");
            Directory.CreateDirectory(tempDir2);
            string tempDir = Path.Combine(OS.GetUserDataDir(), "file_parts", script.Name + script.GetExtensionFromLanguage());
            File.WriteAllText(tempDir, script.Script);
            FileStream fs = new FileStream(tempDir, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.Delete | FileShare.ReadWrite);
            APIObject.UploadFile(result =>
            {
                fs.Dispose();
                if (result.success)
                {
                    scripts.Add($"{APIObject.Settings.APIURL}file/{result.result.UploadData.UserId}/{result.result.UploadData.FileId}");
                    ss.RemoveAt(0);
                    if (ss.Count <= 0)
                    {
                        QuickInvoke.InvokeActionOnMainThread(callback, scripts);
                    }
                    else
                    {
                        UploadScripts(serverScripts, callback, scripts, ss);
                    }
                }
                else
                {
                    Logger.CurrentLogger.Warn("Failed to upload script " + script.Name + script.GetExtensionFromLanguage() + " " + result.message);
                    UploadScripts(serverScripts, callback, scripts, ss);
                }
            }, CurrentUser, CurrentToken, fs);
        }

        public static void UploadWorld(string fullPath, WorldMeta meta, List<string> serverScripts, Action<bool, string> callback = null)
        {
            WorldMeta metaFinal = new WorldMeta(meta.Id, meta.OwnerId, meta.Publicity, meta.Name, meta.Description, meta.ThumbnailURL);
            metaFinal.ServerScripts.AddRange(serverScripts);
            try
            {
                FileStream fs = new FileStream(fullPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Delete | FileShare.Read);
                if (fs.Length > 1048576 * 90)
                {
                    string tempDir = Path.Combine(OS.GetUserDataDir(), "file_parts");
                    Directory.CreateDirectory(tempDir);
                    APIObject.UploadPart(result =>
                    {
                        fs.Dispose();
                        Directory.Delete(tempDir, true);
                        if (result.success)
                            Logger.CurrentLogger.Log("Uploaded world!");
                        else
                            Logger.CurrentLogger.Error($"Failed to upload world! {result.message}");
                        if (callback != null)
                            QuickInvoke.InvokeActionOnMainThread(callback, result.success, result.message);
                    }, CurrentUser, CurrentToken, fs, tempDir, worldMeta: metaFinal);
                }
                else
                {
                    APIObject.UploadWorld(result =>
                    {
                        fs.Dispose();
                        if (result.success)
                            Logger.CurrentLogger.Log("Uploaded world!");
                        else
                            Logger.CurrentLogger.Error($"Failed to upload world! {result.message}");
                        if (callback != null)
                            QuickInvoke.InvokeActionOnMainThread(callback, result.success, result.message);
                    }, CurrentUser, CurrentToken, fs, metaFinal);
                }
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error(e);
                callback?.Invoke(false, e.Message);
            }
        }
    }
}
