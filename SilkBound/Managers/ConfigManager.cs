using Newtonsoft.Json;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Managers
{
    public struct ServerSettings()
    {
        #region Enums
        public enum ServerVisibility
        {
            Public,
            Private,
            //FriendsOnly,
        }
        public enum BossTargeting
        {
            Nearest,
            Furthest,
            LowestHealth,
            HighestHealth,
            Random,

            Default=Nearest
        }
        #endregion

        public bool LogPlayerDisconnections = true;
        public bool ServerBenches = true;
        public bool GhostAfterDeath = true;
        public BossTargeting BossTargetingMethod = BossTargeting.Default;
        [JsonIgnore]
        public ServerVisibility Visibility = ServerVisibility.Private;
    }
    public class Config
    {
        #region Config

        /// <summary>
        /// Online username
        /// </summary>
        public string Username = "Weaver";

        /// <summary>
        /// Default port for hosting/joining-(default, if not specified) servers.
        /// </summary>
        public ushort Port = SilkConstants.PORT;

        /// <summary>
        /// If true, the game will attempt to save (wip lol) and load your save data from the local files instead of the hosts.
        /// </summary>
        public bool UseMultiplayerSaving = true;

        public ServerSettings HostSettings = default;

        #endregion

        #region QOL
        public void SaveToFile(string filename = "cfg")
        {
            ConfigurationManager.SaveToFile(this, filename);
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
        #endregion
    }
    public class ConfigurationManager
    {
        static string FileDirectory = ModFolder.Root.FullName;
        static string Resolve(string path)
        {
            return Path.Combine(FileDirectory, path);
        }

        public static void SaveToFile(Config instance, string fileName = "cfg")
        {
            File.WriteAllText(Resolve($"{fileName}.json"), JsonConvert.SerializeObject(instance, Formatting.Indented));
        }

        public static Config ReadFromFile(string fileName = "cfg")
        {
            if (File.Exists(Resolve($"{fileName}.json")))
                return JsonConvert.DeserializeObject<Config>(File.ReadAllText(Resolve($"{fileName}.json"))) ?? new Config();
            else
                return new Config();
        }
    }
}
