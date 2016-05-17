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

    [JsonObject]
    internal struct ServerList
    {
        [JsonObject]
        internal struct Server
        {
            [JsonProperty]
            public long Adress;
            [JsonProperty]
            public int Port;
        }
        [JsonProperty]
        public List<Server> serverlist;

    }
    internal class Config
    {
        internal string Path { get; set; }
        internal const uint ServerFileLifeTime = 60;
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
                Loging.LogToFile("Cant deserialize config file ("+e.Message+")");
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
        internal static void ServerListSave(SteamKit2.SmartCMServerList serverList)
        {
            if (!File.Exists(SMAForm.ServerLists))
            {
                File.Create(SMAForm.ServerLists);
                System.Threading.Thread.Sleep(1000); //Creating directory delay
            }
            ServerList _serverList = new ServerList(); //Creating list of servers
            _serverList.serverlist = new List<ServerList.Server>();
            foreach (var endPoint in serverList.GetAllEndPoints())
            {

                _serverList.serverlist.Add(new ServerList.Server //Adding every server from CMServerList to our server list
                {
                    Adress = endPoint.Address.Address,
                    Port = endPoint.Port
                });
            }
            bool saved = false;
            lock (SMAForm.ServerLists)
            {
                for(byte i=0;i<3 && !saved;i++) //We use 3 try, becouse sometime file cant be usable
                { 
                    try {
                        File.WriteAllText(SMAForm.ServerLists, JsonConvert.SerializeObject(_serverList));
                        saved = true;
                    } catch(Exception e) {
                        Loging.LogToFile("Cant save server list "+e.Message);
                    }
                }
            }
        }
        internal static void ServerListLoad(SteamKit2.SmartCMServerList serverlist)
        {
            serverlist.Clear();
            if (!File.Exists(SMAForm.ServerLists))
                return;
            ServerList _serverList = new ServerList(); //Creating our server list to save deserialized data
            try { 
            _serverList =  JsonConvert.DeserializeObject<ServerList>(File.ReadAllText(SMAForm.ServerLists));
            } catch { return; }
            foreach (ServerList.Server server in _serverList.serverlist)
            {
                serverlist.TryAdd(new System.Net.IPEndPoint(server.Adress, server.Port));//Adding every serialized server to CMServerList
                
            }
        }
        internal Config() { }
    }

    internal sealed class BotConfig : Config
    {
        [JsonProperty]
        public string Login { get; set; } = null;
        [PasswordPropertyText]
        [JsonProperty]
        public string Password { get; set; } = null;
        [JsonProperty]
        public bool AutoFarm { get; set; } = true;
        [JsonProperty]
        internal string loginKey { get; set; } = null;
        [JsonProperty(Required = Required.DisallowNull)]
        public bool Enabled { get; set; } = true;

        internal BotConfig(string path) : base(path)
        {

        }
        internal BotConfig Load()
        {
            return (BotConfig)(this as Config).Load();
        }
        private BotConfig() { }
    }

    internal sealed class ProgramConfig : Config
    {
        [JsonProperty]
        internal uint CellID { get; private set; } = 0;
        [JsonProperty]
        internal string SomeProperty { get; set; } = "Default";

        private const string ProgramConfigPath = SMAForm.ConfigDirectory+"/Program";
        internal ProgramConfig() : base(ProgramConfigPath)
        {

        }
        internal ProgramConfig Load()
        {
            return (ProgramConfig)(this as Config).Load();
        }

        internal void SetCellID(uint cellID)
        {
            CellID = cellID;
            Save();
        }
    }
}
