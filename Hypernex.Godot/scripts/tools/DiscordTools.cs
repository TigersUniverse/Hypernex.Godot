using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Godot;
using Hypernex.CCK;
using Hypernex.Player;
using HypernexSharp.APIObjects;
using Status = HypernexSharp.APIObjects.Status;
using User = HypernexSharp.APIObjects.User;

namespace Hypernex.Tools
{
#if false
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
        private static Discord.Discord discord = null;
        private static readonly long startTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();

        private static bool ignoreUserRefresh;
        private static readonly Dictionary<string, long> InstanceDateTimes = new();

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
                var userManager = discord.GetUserManager();
                string status = user.Bio.Status.ToString();
                string statusSpaced = GetSpacedStatus(user.Bio.Status);
                discord.GetActivityManager().UpdateActivity(new Activity
                {
                    Name = "Hypernex",
                    Details = $"Playing as {user.Username}",
                    Timestamps = new ActivityTimestamps {Start = startTime},
                    Assets = new ActivityAssets
                    {
                        LargeImage = "logo",
                        SmallImage = status.ToLower(),
                        SmallText = string.IsNullOrEmpty(APITools.CurrentUser.Bio.StatusText)
                            ? statusSpaced
                            : APITools.CurrentUser.Bio.StatusText
                    }
                }, result => { });
            } catch(Exception){}
        }

        public static void StartDiscord()
        {
            try
            {
                if (IsInitialized)
                    return;
                discord = new(DiscordApplicationId, (ulong)CreateFlags.NoRequireDiscord);
                discord.SetLogHook(LogLevel.Debug, (level, msg) =>
                {
                    switch (level)
                    {
                        default:
                        case LogLevel.Debug:
                            Logger.CurrentLogger.Debug($"DiscordGameSDK: {msg}");
                            break;
                        case LogLevel.Info:
                            Logger.CurrentLogger.Log($"DiscordGameSDK: {msg}");
                            break;
                        case LogLevel.Warn:
                            Logger.CurrentLogger.Warn($"DiscordGameSDK: {msg}");
                            break;
                        case LogLevel.Error:
                            Logger.CurrentLogger.Error($"DiscordGameSDK: {msg}");
                            break;
                    }
                });
                discord.GetActivityManager().UpdateActivity(new Activity
                {
                    Name = "Hypernex",
                    Details = "Logging In",
                    Timestamps = new ActivityTimestamps {Start = startTime},
                    Assets = new ActivityAssets {LargeImage = "logo"}
                }, result => { });
                APITools.OnUserRefresh += user =>
                {
                    // if (ignoreUserRefresh)
                        // return;
                    DefaultActivity(user);
                };
                APITools.OnLogout += () =>
                {
                    ignoreUserRefresh = false;
                    InstanceDateTimes.Clear();
                    discord.GetActivityManager().UpdateActivity(new Activity
                    {
                        Name = "Hypernex",
                        Details = "Logging In",
                        Timestamps = new ActivityTimestamps {Start = startTime},
                        Assets = new ActivityAssets {LargeImage = "logo"}
                    }, result => { });
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
                discord.GetActivityManager().UpdateActivity(new Activity
                {
                    Name = "Hypernex",
                    Details = $"Playing as {APITools.CurrentUser.Username}",
                    Timestamps = new ActivityTimestamps {Start = time},
                    State = "Visiting " + worldMeta.Name,
                    Assets = new ActivityAssets
                    {
                        LargeImage = string.IsNullOrEmpty(worldMeta.ThumbnailURL) ? "logo" : worldMeta.ThumbnailURL,
                        LargeText = $"Hosted By {host.Username}",
                        SmallImage = status.ToLower(),
                        SmallText = string.IsNullOrEmpty(APITools.CurrentUser.Bio.StatusText)
                            ? statusSpaced
                            : APITools.CurrentUser.Bio.StatusText
                    }
                }, result => { });
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
                    discord.RunCallbacks();
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