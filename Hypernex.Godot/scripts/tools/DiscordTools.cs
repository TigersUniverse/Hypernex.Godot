using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DiscordRPC;
using DiscordRPC.Logging;
using Godot;
using Hypernex.CCK;
using Hypernex.Player;
using HypernexSharp.APIObjects;
using Status = HypernexSharp.APIObjects.Status;
using User = HypernexSharp.APIObjects.User;

namespace Hypernex.Tools
{
#if GODOT_ANDROID
    internal static class DiscordTools
    {
        public static void StartDiscord()
        {
        }

        internal static void FocusInstance(WorldMeta worldMeta, string id, User host)
        {
        }

        internal static void UnfocusInstance(string id)
        {
        }

        internal static void RunCallbacks()
        {
        }

        internal static void Stop()
        {
        }
    }
#else
    internal static class DiscordTools
    {
        private const long DiscordApplicationId = 1101618498062516254;
        private static DiscordRpcClient discord = null;
        private static readonly long startTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();

        private static bool ignoreUserRefresh;
        private static readonly Dictionary<string, long> InstanceDateTimes = new();

        public class DiscordLogger : ILogger
        {
            public LogLevel Level { get; set; }

            public void Error(string message, params object[] args)
            {
                Logger.CurrentLogger.Error($"DiscordGameSDK: {string.Format(message, args)}");
            }

            public void Info(string message, params object[] args)
            {
                Logger.CurrentLogger.Log($"DiscordGameSDK: {string.Format(message, args)}");
            }

            public void Trace(string message, params object[] args)
            {
                Logger.CurrentLogger.Debug($"DiscordGameSDK: {string.Format(message, args)}");
            }

            public void Warning(string message, params object[] args)
            {
                Logger.CurrentLogger.Warn($"DiscordGameSDK: {string.Format(message, args)}");
            }

        }

        private static bool IsInitialized => discord != null;

        private static string GetSpacedStatus(Status status)
        {
            StringBuilder statusSpaced = new StringBuilder();
            for (int i = 0; i < status.ToString().Length; i++)
            {
                char c = status.ToString()[i];
                if (i != 0 && char.IsUpper(c))
                {
                    statusSpaced.Append(" ");
                    statusSpaced.Append(c);
                }
                else
                    statusSpaced.Append(c);
            }
            return statusSpaced.ToString();
        }

        private static void DefaultActivity(User user)
        {
            try
            {
                string status = user.Bio.Status.ToString();
                string statusSpaced = GetSpacedStatus(user.Bio.Status);
                discord.SetPresence(new RichPresence()
                {
                    Details = $"Playing as {user.Username}",
                    Timestamps = new Timestamps() {StartUnixMilliseconds = (ulong)startTime},
                    Assets = new Assets()
                    {
                        LargeImageKey = "logo",
                        SmallImageKey = status.ToLower(),
                        SmallImageText = string.IsNullOrEmpty(APITools.CurrentUser.Bio.StatusText)
                            ? statusSpaced
                            : APITools.CurrentUser.Bio.StatusText
                    },
                });
            } catch(Exception){}
        }

        public static void StartDiscord()
        {
            try
            {
                if (IsInitialized)
                    return;
                discord = new DiscordRpcClient(DiscordApplicationId.ToString(), autoEvents: false);
                discord.Logger = new DiscordLogger();
                discord.Initialize();
                discord.SetPresence(new RichPresence()
                {
                    Details = "Logging In",
                    Timestamps = new Timestamps() {StartUnixMilliseconds = (ulong)startTime},
                    Assets = new Assets() {LargeImageKey = "logo"},
                });
                APITools.OnUserLogin += user =>
                {
                    // if (ignoreUserRefresh)
                        // return;
                    DefaultActivity(user);
                };
                APITools.OnLogout += () =>
                {
                    ignoreUserRefresh = false;
                    InstanceDateTimes.Clear();
                    discord.SetPresence(new RichPresence()
                    {
                        Details = "Logging In",
                        Timestamps = new Timestamps() {StartUnixMilliseconds = (ulong)startTime},
                        Assets = new Assets() {LargeImageKey = "logo"},
                    });
                };
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
            }
        }

        internal static void FocusInstance(WorldMeta worldMeta, string id, User host)
        {
            try
            {
                if (!IsInitialized)
                    return;
                ignoreUserRefresh = true;
                long time;
                if (InstanceDateTimes.ContainsKey(id))
                    time = InstanceDateTimes[id];
                else
                {
                    time = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                    InstanceDateTimes.Add(id, time);
                }
                string status = APITools.CurrentUser.Bio.Status.ToString();
                string statusSpaced = GetSpacedStatus(APITools.CurrentUser.Bio.Status);
                discord.SetPresence(new RichPresence()
                {
                    Details = $"Playing as {APITools.CurrentUser.Username}",
                    Timestamps = new Timestamps {StartUnixMilliseconds = (ulong)time},
                    State = "Visiting " + worldMeta.Name,
                    Assets = new Assets()
                    {
                        LargeImageKey = string.IsNullOrEmpty(worldMeta.ThumbnailURL) ? "logo" : worldMeta.ThumbnailURL,
                        LargeImageText = $"Hosted By {host.Username}",
                        SmallImageKey = status.ToLower(),
                        SmallImageText = string.IsNullOrEmpty(APITools.CurrentUser.Bio.StatusText)
                            ? statusSpaced
                            : APITools.CurrentUser.Bio.StatusText
                    }
                });
            } catch(Exception){}
        }

        internal static void UnfocusInstance(string id)
        {
            ignoreUserRefresh = false;
            DefaultActivity(APITools.CurrentUser);
            if (InstanceDateTimes.ContainsKey(id))
                InstanceDateTimes.Remove(id);
        }

        internal static void RunCallbacks()
        {
            try
            {
                if (IsInitialized)
                    discord.Invoke();
            } catch(Exception){}
        }

        internal static void Stop()
        {
            try
            {
                if (IsInitialized)
                    discord.Dispose();
            } catch(Exception){}
        }
    }
#endif
}