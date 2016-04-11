using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace SteamMultiAccount
{
    internal sealed class Config
    {
        [JsonProperty]
        public string Login { get; set; } = null;

        [JsonProperty]
        public string Password { get; set; } = null;

        internal string Path { get; set; }
        internal Config(string path)
        {
            Path = path+".json";
            if (!File.Exists(Path)) { 
                Save();
            }
        }

        internal Config Load(Loging logging = null)
        {
            if (string.IsNullOrEmpty(Path))
                return null;

            Config config;
            try {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path));
            } catch(Exception e) {
                Loging.LogToFile("Cant deserialize config file ("+e.Message+")");
                return null;
            }
            config.Path = Path;
            return config;
        }

        internal void Save()
        {
            lock (Path)
            {
                try {
                    File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));
                } catch(Exception e) {
                    Loging.LogToFile("Cant save config ("+e.Message+")");
                }
            }
        }

        internal void Delete()
        {
            if (!File.Exists(Path))
                return;
            lock (Path)
            {
                try {
                    File.Delete(Path);
                } catch(Exception e) {
                    Loging.LogToFile("Cant delete config ("+e.Message+")");
                }
            }
        }
        private Config() { }
    }
}
