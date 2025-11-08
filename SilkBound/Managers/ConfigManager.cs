using Newtonsoft.Json;
using SilkBound.Network.Packets;
using SilkBound.Utils;
using System;
using System.IO;
using System.Reflection;
using UnityEngine.UIElements;

namespace SilkBound.Managers {
    #region Enums
    public enum ServerVisibility {
        Public,
        Private,
        //FriendsOnly,
    }
    public enum BossTargetingMethod {
        Nearest,
        Farthest,
        LowestHealth,
        HighestHealth,
        Random,

        Default = Nearest
    }
    public enum RespawnMode {
        Individual, // players go to their own benches
        Shared, // players go to the last used bench by any player instantly
        PartyDeath, // players go to their own last used bench after last living player dies - ignores GhostAfterDeath
        SharedPartyDeath, // players go to the last bench used by any player after last living player dies - ignores GhostAfterDeath  

        Default = Individual
    }

    public enum NetworkingLayer {
        TCP,
        Steam,
        NamedPipe,

        Default = TCP
    }
    #endregion

    public struct ServerSettings() {
        /// <summary>
        /// Send a message in console when a player disconnects
        /// </summary>
        public bool LogPlayerDisconnections = true;
        public bool ForceHostSaveData = false;
        public BossTargetingMethod BossTargeting = BossTargetingMethod.Default;
        public RespawnMode RespawnMethod = RespawnMode.Default;
        public AuthorityNode LoadGamePermission = AuthorityNode.Any;
        public bool DistributedCocoonSilk = false;

        [Obsolete("Placeholder for future implementation of concept.")]
        [JsonIgnore]
        public ServerVisibility Visibility = ServerVisibility.Private;

        [Obsolete("Placeholder for future implementation of concept.")]
        [JsonIgnore]
        // NOTE: SilkBound will not include a client anticheat (the only way to do this would to make a closed source native binary and theres no way thats happening unless its a huge issue)
        // ^ (cont.) this means that all the anticheat stuff will be performed on the server.
        // ^ (cont. 2) kinda only useful to act as a reminder that a serverside anticheat could increase lag and make it more efficient to turn it off for private coop servers (where the server is also a client)
        public bool AntiCheatEnabled = false;

        //i dont think these need jsonignore but ill do it anyways lol
        [JsonIgnore]
        public bool ServerBenches => RespawnMethod == RespawnMode.SharedPartyDeath || RespawnMethod == RespawnMode.Shared;
        [JsonIgnore]
        public bool GhostAfterDeath {
            get
            {
                Logger.Warn("overwriting respawnmode to use ghosts");
                return false;
            }
        }//RespawnMethod == RespawnMode.SharedPartyDeath || RespawnMethod == RespawnMode.PartyDeath;
    }
    public class Config {
        #region Config


#if !SERVER

        /// <summary>
        /// Online username
        /// </summary>
        public string Username = "Weaver";
        public string SkinName = "red";

        public string HostIP = "0.0.0.0";
        public string ConnectIP = "127.0.0.1";

        public NetworkingLayer NetworkLayer = NetworkingLayer.Default;
#endif
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
        public Skin Skin() => SkinManager.GetOrDefault(SkinName);

        public void SaveToFile(string filename = "config")
        {
            ConfigurationManager.SaveToFile(this, filename);
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
        #endregion
    }
    public class ConfigurationManager {
        static readonly string FileDirectory = ModFolder.Root.FullName;
        static string Resolve(string path)
        {
            return Path.Combine(FileDirectory, path);
        }

        public static void SaveToFile(Config instance, string fileName = "config")
        {
            File.WriteAllText(Resolve($"{fileName}.json"), JsonConvert.SerializeObject(instance, Formatting.Indented));
        }

        public static Config ReadFromFile(string fileName = "config")
        {
            if (File.Exists(Resolve($"{fileName}.json")))
                return JsonConvert.DeserializeObject<Config>(File.ReadAllText(Resolve($"{fileName}.json"))) ?? new Config();
            return new Config();
        }
    }

    public static class LocalProps {
        public static T? Safe<T>(string name, T? fallback = default)
        {
#if DEBUG
            var original = typeof(SilkBound.Generated.Props)
                .GetField(
                    name,
                    BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Static
                )?
                .GetValue(null);

            var converted = original != null ? (T) Convert.ChangeType(original, typeof(T)) : fallback;

            return converted ?? fallback;
#else
            return default(T) ?? fallback;
#endif
        }
    }
}
