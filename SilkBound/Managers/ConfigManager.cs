using Newtonsoft.Json;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Managers
{
    public class Config
    {
        #region Config

        /// <summary>
        /// Online username
        /// </summary>
        public string Username = "Weaver";

        /// <summary>
        /// Default port for hosting/joining-(if not specified) servers.
        /// </summary>
        public ushort Port = SilkConstants.PORT;

        /// <summary>
        /// If true, the game will attempt to save (wip lol) and load your save data from the local files instead of the hosts.
        /// </summary>
        public bool UseMultiplayerSaving = true;

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
