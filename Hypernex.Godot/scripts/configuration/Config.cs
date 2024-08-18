using System.Collections.Generic;
using Hypernex.Configuration.ConfigMeta;
using Tomlet.Attributes;

namespace Hypernex.Configuration
{
    public class Config
    {
        [TomlProperty("SelectedMicrophone")]
        public string SelectedMicrophone { get; set; }

        [TomlProperty("DownloadThreads")]
        public int DownloadThreads { get; set; } = 50;

        [TomlProperty("MaxMemoryStorageCache")]
        public int MaxMemoryStorageCache { get; set; } = 5120;
        
        [TomlProperty("SavedServers")]
        public List<string> SavedServers { get; set; } = new(){"play.hypernex.dev"};

        [TomlProperty("SavedAccounts")]
        public List<ConfigUser> SavedAccounts { get; set; } = new();
        
        [TomlProperty("UseTrustedURLs")]
        public bool UseTrustedURLs { get; set; } = true;
        
        [TomlProperty("TrustedURLs")]
        public List<string> TrustedURLs { get; set; } = new()
        {
            "https://discordapp.com",
            "https://discord.com",
            "https://cdn.discordapp.com",
            "https://vrcdn.live",
            "https://stream.vrcdn.live"
        };

        public ConfigUser GetConfigUserFromUserId(string userid)
        {
            foreach (ConfigUser savedAccount in SavedAccounts)
                if (savedAccount.UserId == userid)
                    return savedAccount;
            return null;
        }
    }
}