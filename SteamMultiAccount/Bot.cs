using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;

namespace SteamMultiAccount
{
    internal sealed class Bot
    {
        private const ushort CallbackSleep = 1000;

        internal bool isRunning, needAuthCode, needTwoFactorAuthCode;
        private bool isManualDisconnect = false;
        private string authCode = string.Empty;
        private string twoFactorAuthCode = string.Empty;
        private readonly string sentryPath;
        private string _logboxText;

        internal static readonly Dictionary<string, string> Commands = new Dictionary<string, string>(){
            { "Nickname ", "Set bot nickname = arg[1]"},
            {"FriendList", "Show friend list"},
            {"Send ","Send message to friend with steamid = arg[1]" },
            {"SellCards","Sell all trading cards in inventory" }
        };
        internal string Status = string.Empty;

        internal        ulong[] CurrentFarming;
        internal Timer timer;
        internal static uint cellID = 0;
        internal static readonly uint loginID = MsgClientLogon.ObfuscationMask;
        internal static readonly Dictionary<string, Bot> Bots = new Dictionary<string, Bot>();
        internal        readonly List<SteamID> FriendList = new List<SteamID>();
        internal readonly string BotName;
        internal readonly string BotPath;
        internal          Config BotConfig;
        internal readonly Loging logging;
        internal readonly SteamClient steamClient;
        internal readonly SteamUser steamUser;
        internal readonly SteamFriends steamFriends;
        internal readonly CallbackManager callbackManager;
        internal          WebBot webBot;

        internal Bot(string botName)
        {
            if (string.IsNullOrEmpty(botName))
                return;
            BotName = botName;
            BotPath = Path.Combine(SMAForm.ConfigDirectory, BotName);
            sentryPath = System.IO.Path.Combine(SMAForm.BotsData, BotName + ".bin");

            logging = new Loging(_logboxText);
            BotConfig = new Config(BotPath);
            steamClient = new SteamClient();
            steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();
            callbackManager = new CallbackManager(steamClient);
            webBot = new WebBot();

            BotConfig = BotConfig.Load(logging);

            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachinAuth);
            callbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnLoginKey);
            callbackManager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);

            callbackManager.Subscribe<SteamFriends.FriendMsgCallback>(OnFriendMessage);
            callbackManager.Subscribe<SteamFriends.FriendsListCallback>(OnFriendList);

            if (Bots.ContainsKey(BotName))
                return;
            Bots[BotName] = this;

            Restart();
        }
        internal void Restart()
        {
            Config conf = BotConfig.Load(logging);
            if (conf == BotConfig)
                return;

            BotConfig = conf;
            if (!BotConfig.Enabled)
            {
                Log("Bot disabled", LogType.Info);
                Status = "Disabled";
                return;
            }
            Log("Bot enabled", LogType.Info);

            NetDebug();

            if (cellID == 0 && BotConfig.cellID != 0)
                cellID = BotConfig.cellID;

            RefreshCMs(cellID).Wait();

            Start().Forget();
        }
        internal async Task<string> Response(string message)
        {
            if (string.IsNullOrEmpty(message))
                return null;
            string Ret = string.Empty;

            if (needAuthCode && message.Length == 5){
                needAuthCode = false;
                authCode = message;
                steamClient.Connect();
                return null;
            }
            if (needTwoFactorAuthCode && message.Length == 5){
                needTwoFactorAuthCode = false;
                twoFactorAuthCode = message;
                steamClient.Connect();
                return null;
            }

            // TODO:Loot giving function
            if (message.Contains(" "))
            {
                string[] args = message.Split(' ');
                switch (args[0])
                {
                    case "Nickname":
                        await steamFriends.SetPersonaName(message.Substring("nickname ".Length));
                        Ret = ($"Nickname changed to \"{message.Substring("nickname ".Length)}\"");
                        break;
                    case "Send":
                        ulong steamid;
                        ulong.TryParse(args[1], out steamid);
                        steamFriends.SendChatMessage(steamid, EChatEntryType.ChatMsg, message.Substring((args[0]+args[1]).Length+2));
                        break;
                    default:
                        Ret = "Hello suchechka";
                        break;
                }
            }
            else
                switch (message)
                {
                    case "FriendList":
                        Ret = FriendListShow();
                        break;
                    case "SellCards":
                        Sellcards();
                        break;
                    default:
                        Ret = "Hello suchechka";
                        break;
                }
            Log(Ret, LogType.Info);
            return Ret;
        }
        internal async void Response(string message, SteamID Sender)
        {
            steamFriends.SendChatMessage(Sender, EChatEntryType.ChatMsg, await Response(message));
        }
        internal void FarmGame(ulong[] appIDs)
        {
            CurrentFarming = appIDs;
            var req = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
            foreach (ulong appid in appIDs)
            {
                if (appid == 0)
                    continue;
                req.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed() { game_id = new GameID(appid) });
            }
            steamClient.Send(req);
        }
        internal void FarmGame(ulong appID)
        {
            FarmGame(new ulong[] { appID });
        }
        internal void SendMessage(SteamID steamID, string message)
        {
            if (steamID == null || string.IsNullOrEmpty(message))
                return;
            steamFriends.SendChatMessage(steamID, EChatEntryType.ChatMsg, message);
        }
        /*
        //
        // Commands
        //  
        */
        internal string FriendListShow()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var friend in FriendList)
            {
                sb.Append(Environment.NewLine + "[" + friend.ConvertToUInt64() + "] " + steamFriends.GetFriendPersonaName(friend));
            }
            return sb.ToString();
        }
        internal async Task Sellcards()
        {
            List<WebBot._Item> ItemList = await webBot.GetTraddableItems().ConfigureAwait(false);
            if (ItemList == null)
                return;
            foreach (WebBot._Item Item in ItemList)
            {
                Log(await webBot.SellItem(Item).ConfigureAwait(false), LogType.Info);
            }
        }

        internal void Farm()
        {
            Log("Refresh game to farm", LogType.Info);

            webBot.RefreshGamesToFarm().Wait();

            if (!(webBot.appidToFarmSolo.Any() || webBot.appidToFarmMulti.Any()))//If we dont have anything to farm
                return;



            if (webBot.appidToFarmMulti.Count > 1)
            {
                Status = $"Farming cards {webBot.appidToFarmMulti.Count} games left";
                FarmGame(webBot.appidToFarmMulti.ToArray());//Farm multi if we have more than 1 game without ability to farm cards
            }
            else
            {
                if (webBot.appidToFarmMulti.Any())
                    webBot.appidToFarmSolo.Add(webBot.appidToFarmMulti.First());//Else add our list to farm multi to solo farm list
                Status = $"Farming cards {webBot.appidToFarmSolo.Count} games left";
                FarmGame(webBot.appidToFarmSolo.First());//And farm first game from list
            }

        }
        /*
        //
        // Callbacks
        //
        */
        internal async void OnConnected(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                isRunning = false;
                Log("Cant connect to steam " + callback.Result,LogType.Error);
                return;
            }

            Log("Connected to steam",LogType.Info);
            byte[] sentryHash = null;
            if (File.Exists(sentryPath))
            {
                // if we have a saved sentry file, read and sha-1 hash it
                byte[] sentryFile = File.ReadAllBytes(sentryPath);
                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }
            if (string.IsNullOrEmpty(BotConfig.Login) || string.IsNullOrEmpty(BotConfig.Password))
                return;

            steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = BotConfig.Login,
                Password = BotConfig.Password,
                AuthCode = authCode,
                TwoFactorCode = twoFactorAuthCode,
                LoginID = loginID,
                LoginKey = BotConfig.loginKey,
                ShouldRememberPassword = true,
                SentryFileHash = sentryHash
            });
            authCode = string.Empty;
            twoFactorAuthCode = string.Empty;
        }
        internal async void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            if (needAuthCode || needTwoFactorAuthCode)
                return;
            Log("Disconnected from steam",LogType.Info);
            if (!isManualDisconnect)
            {
                Log("Reconnecting to steam", LogType.Info);
                System.Threading.Thread.Sleep(5000);
                Start().Forget();
            }
            isRunning = false;
        }
        internal async void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                if (callback.Result == EResult.AccountLogonDenied)
                {
                    Log("Auth code was sended on your email.", LogType.Warning);
                    Log("Please type your auth code.", LogType.Warning);
                    needAuthCode = true;
                    return;
                }
                if (callback.Result == EResult.AccountLogonDeniedNeedTwoFactorCode)
                {
                    Log("Please type your two factor auth code.", LogType.Warning);
                    needTwoFactorAuthCode = true;
                    return;
                }
                if (callback.Result == EResult.InvalidPassword)
                {
                    if (BotConfig.loginKey != string.Empty) { 
                        BotConfig.loginKey = null;
                        BotConfig.Save();
                        return;
                    }
                }
                Log("Unable to logon to steam (" + callback.Result + ") " + callback.ExtendedResult,LogType.Error);
                isRunning = false;
                return;
            }
            BotConfig.SetCellId(callback.CellID);
            Log("Successfully logged on!",LogType.Info);
            Status = "Running";

            webBot.Init(this, callback.WebAPIUserNonce);
            timer = new Timer(async e => Farm(), null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
        }
        internal async void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            timer.Dispose();
            Log("Logged off of steam (" + callback.Result + ")",LogType.Info);
        }
        internal async void OnMachinAuth(SteamUser.UpdateMachineAuthCallback callback)
        {
            Log("Updating sentry file", LogType.Info);

            int fileSize;
            byte[] sentryHash;
            string Path = System.IO.Path.Combine(SMAForm.BotsData, BotName + ".bin");
            using (var fs = File.Open(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Seek(callback.Offset, SeekOrigin.Begin);
                fs.Write(callback.Data, 0, callback.BytesToWrite);
                fileSize = (int) fs.Length;
                fs.Seek(0, SeekOrigin.Begin);
                using (var sha = new SHA1CryptoServiceProvider())
                {
                    sentryHash = sha.ComputeHash(fs);
                }
            }
            steamUser.SendMachineAuthResponse(
                new SteamUser.MachineAuthDetails
                {
                    JobID = callback.JobID,
                    FileName = callback.FileName,
                    BytesWritten = callback.BytesToWrite,
                    FileSize = fileSize,
                    Offset = callback.Offset,
                    Result = EResult.OK,
                    LastError = 0,
                    OneTimePassword = callback.OneTimePassword,
                    SentryFileHash = sentryHash
                });
            Log("Updating successfully", LogType.Info);
        }
        internal async void OnLoginKey(SteamUser.LoginKeyCallback callback)
        {
            BotConfig.loginKey = callback.LoginKey;
            BotConfig.Save();
            Log("Login key saved", LogType.Info);
        }
        internal async void OnAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            steamFriends.SetPersonaState(EPersonaState.Online);
        }
        internal async void OnFriendMessage(SteamFriends.FriendMsgCallback callback)
        {
            if(callback.EntryType == EChatEntryType.ChatMsg)
            Response(callback.Message, callback.Sender);
        }
        internal async void OnFriendList(SteamFriends.FriendsListCallback callback)
        {
            if (!callback.FriendList.Any())
                return;
            foreach (var friend in callback.FriendList)
            {
                if (friend.Relationship == EFriendRelationship.RequestRecipient)
                { 
                    steamFriends.AddFriend(friend.SteamID);
                    Log(steamFriends.GetFriendPersonaName(friend.SteamID)+" was added to your friends list",LogType.Info);
                }
                if (friend.SteamID.IsIndividualAccount)
                    FriendList.Add(friend.SteamID);
            }
        }
        /*
        //
        // Services
        //
        */
        private async Task Start()
        {
            if (!isRunning)
            {
                isRunning = true;
                Task.Run(() => CallbacksHandler()).Forget();
            }
            Log("Connecting to steam...", LogType.Info);
            Status = "Connecting";

            steamClient.Connect();
        }
        private void NetDebug()
        {
            if (!Listener.NetHookAlreadyInitialized && Directory.Exists(SMAForm.DebugDirectory))
            {
                try
                {
                    steamClient.DebugNetworkListener = new NetHookNetworkListener(SMAForm.DebugDirectory);
                    Listener.NetHookAlreadyInitialized = true;
                }
                catch (Exception e)
                {
                    Log(e.Message,LogType.Error);
                }
            }
        }
        private void CallbacksHandler()
        {
            while (isRunning || steamClient.IsConnected)
            {
                callbackManager.RunWaitCallbacks(TimeSpan.FromMilliseconds(CallbackSleep));
            }
        }
        internal async Task RefreshCMs(uint cellID)
        {
            if (File.Exists(SMAForm.ServerLists) &&
                DateTime.Now.Subtract(File.GetLastWriteTime(SMAForm.ServerLists)) <
                TimeSpan.FromMinutes(Config.ServerFileLifeTime))
            {
                Config.ServerListLoad(CMClient.Servers);
                if (CMClient.Servers.GetAllEndPoints().Any())
                {
                    Log("CM servers loaded from cache", LogType.Info);
                    return;
                }
            }

            bool initialized = false;
            for (byte i = 0; i < 3 && !initialized; i++)
            {
                try
                {
                    Log("Loading list of CM servers", LogType.Info);
                    await SteamDirectory.Initialize(cellID).ConfigureAwait(false);
                    Config.ServerListSave(CMClient.Servers);
                    initialized = true;
                }
                catch (Exception e)
                {
                    Log("Cant refresh list of CMs " + e.Message, LogType.Error);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
            if (initialized)
                Log("Successfully refresh list of CM servers", LogType.Info);
        }
        internal void Log(string message, LogType type, [CallerMemberName] string functionName = "")
        {
            if (string.IsNullOrEmpty(message))
                return;
            logging.Log(message, type, out _logboxText, functionName);
        }
        internal string getLogBoxText()
        {
            return _logboxText;
        }
        internal void Delete()
        {
            BotConfig.Delete();
            Bots.Remove(BotName);
        }


    }

    internal static class Utilities
    {
        internal static void Forget(this Task task) { }
    }
}
