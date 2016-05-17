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
using System.Text.RegularExpressions;

namespace SteamMultiAccount
{
    enum StatusEnum
    {
        Connecting,
        Connected,
        Disabled,
        Farming,
        RefreshGamesToFarm
    }
    internal sealed class Bot
    {
        internal static readonly string[] StatusString =
        {
            "Connecting",
            "Connected",
            "Disabled",
            "Farming",
            "Refresh games to farm"
        };

        private const ushort CallbackSleep = 1000;

        internal bool isRunning, initialized, needAuthCode, needTwoFactorAuthCode,Restarting; // Bot flag
        private bool isManualDisconnect; // Private bot flag
        private string authCode, twoFactorAuthCode; //Temp strings
        private readonly string sentryPath;
        private string _logboxText;
        internal List<uint> AlreadyOwnedGames;
        internal delegate Task<string> MyDelegate(string[] args);
        internal static readonly string[] CommandsKeys = {"Nickname", "Sellcards","RefreshCMs"};
        internal        readonly Dictionary<string, MyDelegate> Commands;
        internal StatusEnum Status;

        internal                 ulong[] CurrentFarming;
        internal                 Timer timer;
        internal static readonly uint loginID = MsgClientLogon.ObfuscationMask;
        internal static readonly Dictionary<string, Bot> Bots = new Dictionary<string, Bot>();
        internal static          ProgramConfig ProgramConfig;
        internal        readonly List<SteamID> FriendList = new List<SteamID>();
        internal        readonly string BotName;
        internal        readonly string BotPath;
        internal                 BotConfig BotConfig;
        internal        readonly Loging logging;
        internal        readonly SteamClient steamClient;
        internal        readonly SteamUser steamUser;
        internal        readonly SteamFriends steamFriends;
        internal        readonly SteamApps steamApps;
        internal        readonly CallbackManager callbackManager;
        internal                 WebBot webBot;
        internal        readonly CustomHandler customHandler;

        internal Bot(string botName)
        {
            if (string.IsNullOrEmpty(botName))
                return;
            BotName = botName;
            BotPath = Path.Combine(SMAForm.ConfigDirectory, BotName);
            sentryPath = Path.Combine(SMAForm.BotsData, BotName + ".bin");
            AlreadyOwnedGames = new List<uint>();
            Commands = new Dictionary<string, MyDelegate>()
            {
                {CommandsKeys[0], ChangeNickname},
                {CommandsKeys[1], Sellcards},
                {CommandsKeys[2], RefreshCMs }
            };

            logging = new Loging(_logboxText);
            BotConfig = new BotConfig(BotPath);
            steamClient = new SteamClient();
            steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();
            steamApps = steamClient.GetHandler<SteamApps>();
            callbackManager = new CallbackManager(steamClient);
            webBot = new WebBot();
            customHandler = new CustomHandler();

            BotConfig = BotConfig.Load();

            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachinAuth);
            callbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnLoginKey);
            callbackManager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);

            callbackManager.Subscribe<SteamFriends.FriendMsgCallback>(OnFriendMessage);
            callbackManager.Subscribe<SteamFriends.FriendsListCallback>(OnFriendList);

            callbackManager.Subscribe<SteamApps.GuestPassListCallback>(OnGuestPass);
            callbackManager.Subscribe<SteamApps.FreeLicenseCallback>(OnFreeLicense);

            // TODO: Join to game aways on sites
            // TODO: Smart command acception, like cortana

            if (Bots.ContainsKey(BotName))
                return;
            Bots[BotName] = this;
            if (ProgramConfig == null)
            {
                ProgramConfig = new ProgramConfig();
                ProgramConfig = ProgramConfig.Load();
            }

            Restart();
        }

        internal void Restart(bool Force = false)
        {
            BotConfig conf = BotConfig.Load();
            if (conf == BotConfig)
                return;

            if (steamClient.IsConnected)
            {
                isManualDisconnect = true;
                steamClient.Disconnect();
                Restarting = true;
                return;
            }

            initialized = false;
            if (!Force)
            {
                BotConfig = conf;
                if (!BotConfig.Enabled)
                {
                    Log("Bot disabled", LogType.Info);
                    Status = StatusEnum.Disabled;
                    initialized = true;
                    return;
                }
                Log("Bot enabled", LogType.Info);
            }

            NetDebug();

            Status = StatusEnum.Connecting;
            RefreshCMs(ProgramConfig.CellID).Wait();

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
            if (message.Contains('-'))
            {
                string[] keys = message.Split((char[]) null,StringSplitOptions.RemoveEmptyEntries);
                    foreach (string key in keys)
                    {
                        if (isKey(key))
                            await KeyActivate(new string[] { key }).ConfigureAwait(false);
                    }
                return Ret;
            }

            // TODO: Ingame idling function
            // TODO: Play dota like bot
            // TODO: Creating account if cant connect
            // TODO: Game adding
            // TODO: Game purchasing
            // TODO: Keys activation
            // TODO: Loot trading
            string[] args = message.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (!args.Any())
                return null;

            if (!Commands.ContainsKey(args[0]))
                Ret = "Invalid command";
            else
            {
                if (args.Length > 1)
                {
                    string[] _args = new string[args.Length - 1];
                    for (int i = 0; i < args.Length - 1; i++)
                        _args[i] = args[i + 1];

                    Ret = await Commands[args[0]](_args).ConfigureAwait(false);
                }
                else Ret = await Commands[args[0]](null).ConfigureAwait(false);
            }

            if(!string.IsNullOrEmpty(Ret))
                Log(Ret, LogType.Info);

            return Ret;
        }
        internal async void Response(string message, SteamID Sender)
        {
            string ret = await Response(message).ConfigureAwait(false);
            if (string.IsNullOrEmpty(ret))
                return;
            steamFriends.SendChatMessage(Sender, EChatEntryType.ChatMsg, ret);
        }
        internal void FarmGame(ulong[] appIDs)
        {
            if (appIDs.Length > 1 && appIDs[0] != 0)
                CurrentFarming = appIDs;
            else
                CurrentFarming = new ulong[1];

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
            FarmGame(new [] { appID });
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
        internal string FriendListShow(string[] args=null)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var friend in FriendList)
            {
                sb.Append(Environment.NewLine + "[" + friend.ConvertToUInt64() + "] " + steamFriends.GetFriendPersonaName(friend));
            }
            return sb.ToString();
        }
        internal async Task<string> Sellcards(string[] args = null)
        {
            List<WebBot._Item> ItemList = await webBot.GetTraddableItems().ConfigureAwait(false);
            if (ItemList == null)
                return "Doesnt have any items to sell";
            foreach (WebBot._Item Item in ItemList)
            {
                Log(await webBot.SellItem(Item).ConfigureAwait(false), LogType.Info);
            }
            return "All cards sold";
        }
        internal async Task<string> ChangeNickname(string[] args)
        {
            await steamFriends.SetPersonaName(args[0]);
            return "Nickname changed to " + args[0];
        }
        internal async Task<string> KeyActivate(string[] args)
        {
            // TODO: Check key activation algorithm
            if (!args.Any())
                return "Empty key";

            uint appID;
            CustomHandler.PurchaseResponseCallback callback;
            try {
                callback = await customHandler.KeyActivate(args[0]).ConfigureAwait(false);
            } catch (Exception e) { Log("Cant get key activation callback: " + e.Message, LogType.Error); return null; }
            switch (callback.PurchaseResult)
            {
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.OK:
                    return "Key was successfully activate";
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.AlreadyOwned:
                    if (callback.Items.Any())
                        AlreadyOwnedGames.Add(callback.Items.First().Key);
                    break;
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.DuplicatedKey:
                    Log("Key (" + args[0] + ") is duplicated", LogType.Warning);
                    return "Key is duplicated";
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.InvalidKey:
                    return "Key is invalid";
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.Unknown:
                    return "Unknown error";
            }
            appID = callback.Items.First().Key;
            foreach (Bot bot in Bots.Values)
            {
                if (bot == this)
                    continue;

                callback = null;
                try { callback = await bot.customHandler.KeyActivate(args[0]).ConfigureAwait(false);
                } catch (Exception e) { Log("Cant get key activation callback: " + e.Message, LogType.Error); return null; }
                if (callback == null)
                    continue;

                if (callback.Result == EResult.OK)
                    switch (callback.PurchaseResult)
                    {
                        case CustomHandler.PurchaseResponseCallback.EPurchaseResult.OK:
                            return null;
                        case CustomHandler.PurchaseResponseCallback.EPurchaseResult.AlreadyOwned:
                            if (callback.Items.Any())
                                AlreadyOwnedGames.Add(callback.Items.First().Key);
                            break;
                    }
            }
            return "Complete";
        }
        internal void Farm()
        {
            Log("Refresh games to farm", LogType.Info);
            Status = StatusEnum.RefreshGamesToFarm;

            webBot.RefreshGamesToFarm().Wait();

            if (!(webBot.appidToFarmSolo.Any() || webBot.appidToFarmMulti.Any())) //If we dont have anything to farm
            { 
                Status = StatusEnum.Connected;
                FarmGame(0);
                return;
            }

            if (webBot.appidToFarmMulti.Count > 1)
            {
                //Status = $"Farming cards {webBot.appidToFarmMulti.Count} games left";
                Status = StatusEnum.Farming;
                FarmGame(webBot.appidToFarmMulti.ToArray());//Farm multi if we have more than 1 game without ability to farm cards
            }
            else
            {
                if (webBot.appidToFarmMulti.Any())
                    webBot.appidToFarmSolo.Add(webBot.appidToFarmMulti.First());//Else add our list to farm multi to solo farm list
                //Status = $"Farming cards {webBot.appidToFarmSolo.Count} games left";
                Status = StatusEnum.Farming;
                FarmGame(webBot.appidToFarmSolo.First());//And farm first game from list
            }
        }
        internal void PauseResumeFarm()
        {
            if (!steamClient.IsConnected || !isRunning)
                return;

            timer = new Timer(e => Farm());
            if (Status == StatusEnum.Farming)
            {
                timer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMinutes(15));
                FarmGame(0);
                Status = StatusEnum.Connected;
            }
            else
                timer.Change(TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(15));
        }
        internal void PauseResume()
        {
            if (steamClient.IsConnected && isRunning)
            {
                isManualDisconnect = true;
                if (steamUser.SteamID != null)
                    steamUser.LogOff();
                steamClient.Disconnect();
            }
            else
                Restart(true);
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
            Status = StatusEnum.Connected;
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
                Thread.Sleep(5000);
                Start().Forget();
                return;
            }
            if (Restarting)
            {
                Restarting = false;
                Restart();
                return;
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
                else if (callback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
                {
                    Log("Please type your two factor auth code.", LogType.Warning);
                    needTwoFactorAuthCode = true;
                    return;
                }
                else if (callback.Result == EResult.InvalidPassword)
                {
                    if (BotConfig.loginKey != string.Empty) { 
                        BotConfig.loginKey = null;
                        BotConfig.Save();
                        return;
                    }
                }
                else if (callback.Result == EResult.NoConnection)
                {
                    Log("No connection", LogType.Warning);
                }
                else
                Log("Unable to logon to steam (" + callback.Result + ") " + callback.ExtendedResult,LogType.Error);
                isRunning = false;
                initialized = true;
                Status = StatusEnum.Disabled;
                return;
            }
            ProgramConfig.SetCellID(callback.CellID);
            Log("Successfully logged on!",LogType.Info);
            Status = StatusEnum.Connected;

            webBot.Init(this, callback.WebAPIUserNonce).Wait();
            initialized = true;
            FarmTimerReset();
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
        internal async void OnFreeLicense(SteamApps.FreeLicenseCallback callback)
        {
            if (callback == null || callback.Result != EResult.OK)
                return;

            Log("Free game(s) added " + callback.GrantedApps.ToArray(), LogType.Info);
        }
        internal async void OnGuestPass(SteamApps.GuestPassListCallback callback)
        {
            if (callback == null || callback.Result != EResult.OK || callback.CountGuestPassesToRedeem == 0 || callback.GuestPasses.Count == 0)
                return;

            bool AcceptedSomething = false;
            foreach (KeyValue guestPass in callback.GuestPasses)
            {
                ulong gID = guestPass["gid"].AsUnsignedLong();
                if (gID == 0)
                    continue;

                Log("Acepting gift(" + gID + ") from ", LogType.Info);
                if (await webBot.AcceptGift(gID).ConfigureAwait(false))
                {
                    Log("Success", LogType.Info);
                    AcceptedSomething = true;
                }
            }
            if (AcceptedSomething)
                Farm();
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
            steamClient.Connect();
        }
        private void FarmTimerReset()
        {
            timer = new Timer(e => Farm());
            TimeSpan startTime = (BotConfig.AutoFarm ? TimeSpan.Zero : TimeSpan.FromMilliseconds(-1));
            timer.Change(startTime, TimeSpan.FromMinutes(15));
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
                try {
                    callbackManager.RunWaitCallbacks(TimeSpan.FromMilliseconds(CallbackSleep));
                } catch (Exception e) {
                    Log("Cant run wait callback: " + e.Message, LogType.Error);
                }
            }
        }
        internal async Task<string> RefreshCMs(string[] args)
        {
            await RefreshCMs(ProgramConfig.CellID,true).ConfigureAwait(false);
            return "CM servers was refreshed";
        }
        internal async Task RefreshCMs(uint _cellID,bool WithoutCache = false)
        {
            if (File.Exists(SMAForm.ServerLists) &&
                DateTime.Now.Subtract(File.GetLastWriteTime(SMAForm.ServerLists)) <
                TimeSpan.FromMinutes(Config.ServerFileLifeTime) && !WithoutCache)
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

                    //await SteamDirectory.Initialize(cellID).ConfigureAwait(false);
                    var loadServerTask = SteamDirectory.Initialize(_cellID);
                    loadServerTask.Wait();
                    if (loadServerTask.IsCompleted)
                    {
                        Config.ServerListSave(CMClient.Servers);
                        initialized = true;
                    }
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
        internal bool isKey(string Key)
        {
            Match First = Regex.Match(Key, @"[0-9,a-z,A-Z]{5}-[0-9,a-z,A-Z]{5}-[0-9,a-z,A-Z]{5}");
            if (First.Success)
                return true;
            return false;
        }
    }

    internal static class Utilities
    {
        internal static void Forget(this Task task) { }
    }
}
