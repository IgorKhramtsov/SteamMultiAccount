using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
    internal sealed class Config
    {
        [JsonProperty]
        public string Login { get; set; } = null;

        [JsonProperty]
        public string Password { get; set; } = null;

        [JsonProperty]
        internal uint cellID { get; set; } = 0;
        internal string Path { get; set; }
        internal const uint ServerFileLifeTime = 60;
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
            _serverList =  JsonConvert.DeserializeObject<ServerList>(File.ReadAllText(SMAForm.ServerLists));
            foreach (ServerList.Server server in _serverList.serverlist)
            {
                serverlist.TryAdd(new System.Net.IPEndPoint(server.Adress, server.Port));//Adding every serialized server to CMServerList
            }
        }

        internal void SetCellId(uint _cellID)
        {
            cellID = _cellID;
            Save();
        }
        private Config() { }
    }
}
