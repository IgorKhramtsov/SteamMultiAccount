using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;

namespace SteamMultiAccount
{
    internal class Config
    {
        internal string Path { get; set; }
        internal Config(string path)
        {
            Path = path+".json";
            if (!File.Exists(Path)) { 
                Save();
            }
        }

        internal object Load()
        {
            if (string.IsNullOrEmpty(Path))
                return null;
            
            try {
                var config = JsonConvert.DeserializeObject(File.ReadAllText(Path),this.GetType());
                if (config != null)
                    (config as Config).Path = Path;
                return config;
            } catch(Exception e) {
                Logging.LogToFile("Cant deserialize config file ("+e.Message+")");
                return null;
            }
        }
        internal void Save()
        {
            lock (Path)
            {
                try {
                    File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));
                } catch(Exception e) {
                    Logging.LogToFile("Cant save config ("+e.Message+")");
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
                    Logging.LogToFile("Cant delete config ("+e.Message+")");
                }
            }
        }
        internal Config() { }
    }

    internal sealed class BotConfig : Config
    {
        [JsonProperty]
        public string Login { get; set; } = null;
        [PasswordPropertyText(true)]
        [JsonProperty]
        public string Password { get; set; } = null;
        [JsonProperty]
        public bool AutoFarm { get; set; } = true;
        [JsonProperty]
        internal string loginKey { get; set; } = null;
        [JsonProperty(Required = Required.DisallowNull)]
        public bool Enabled { get; set; } = true;
        [JsonProperty]
        public bool FarmOffline { get; set; } = false;
        [JsonProperty]
        public string SteamAuthCode { get; set; } = "";
        [JsonProperty]
        public uint CellID { get;private set; } = 0;

        internal BotConfig(string path) : base(path)
        {

        }
        internal BotConfig Load()
        {
            return (BotConfig)(this as Config).Load();
        }
        internal void SetCellID(uint cellID)
        {
            CellID = cellID;
            Save();
        }
        private BotConfig() { }
    }

    internal sealed class ProgramConfig : Config
    {
        [JsonProperty]
        internal int SimultaneousGamesFarming { get; set; } = 30;

        private const string ProgramConfigPath = SMAForm.ConfigDirectory+"/Program";
        internal ProgramConfig() : base(ProgramConfigPath)
        {

        }
        internal ProgramConfig Load()
        {
            return (ProgramConfig)(this as Config).Load();
        }
    }
}
