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

    struct sWallet
    {
        internal ECurrencyCode Curency;
        internal int Balance;
        internal bool HasWallet;
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
        internal static readonly string[] CommandsKeys = {"Nickname", "Sellcards","RefreshCMs","Watch","UnWatch","Buy"};
        internal        readonly Dictionary<string, MyDelegate> Commands;
        internal StatusEnum Status;
        internal sWallet Wallet;

        internal                 ulong[] CurrentFarming;
        internal                 Timer timer;
        internal static readonly uint loginID = MsgClientLogon.ObfuscationMask;
        internal static readonly Dictionary<string, Bot> Bots = new Dictionary<string, Bot>();
        internal static          ProgramConfig ProgramConfig;
        internal        readonly List<SteamID> FriendList = new List<SteamID>();
        internal        readonly string BotName;
        internal        readonly string BotPath;
        internal                 BotConfig BotConfig;
        internal        readonly Logging logging;
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
                {CommandsKeys[2], RefreshCMs },
                {CommandsKeys[3], WatchBroadcastCommand},
                {CommandsKeys[4], UnWatchBroadcastCommand},
                {CommandsKeys[5], BuyApps}
            };

            logging = new Logging(_logboxText);
            BotConfig = new BotConfig(BotPath);
            steamClient = new SteamClient();
            steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();
            steamApps = steamClient.GetHandler<SteamApps>();
            callbackManager = new CallbackManager(steamClient);
            webBot = new WebBot();
            customHandler = new CustomHandler();

            BotConfig = BotConfig.Load();

            steamClient.AddHandler(customHandler);

            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachinAuth);
            callbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnLoginKey);
            callbackManager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);
            callbackManager.Subscribe<SteamUser.WalletInfoCallback>(OnWalletInfo);

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

            if (needAuthCode && message.Length == 5){ // if we must handle auth code
                needAuthCode = false;
                authCode = message;
                steamClient.Connect();
                return null;
            }
            if (needTwoFactorAuthCode && message.Length == 5){ // if we must handle two factor auth code
                needTwoFactorAuthCode = false;
                twoFactorAuthCode = message;
                steamClient.Connect();
                return null;
            }
            if (message.Contains('-')) // Check if request can be key
            {
                string[] keys = message.Split((char[]) null,StringSplitOptions.RemoveEmptyEntries); // Split keys
                if(isKey(keys.First()))//if first element is key, then lets think all elements are keys
                Ret =  await KeyActivate(keys).ConfigureAwait(false);
            }

            // TODO: Ingame idling function
            // TODO: Play dota like bot
            // TODO: Creating account if cant login
            // TODO: Game adding
            // TODO: Game purchasing - CHECK
            // TODO: Loot trading
            if (string.IsNullOrEmpty(Ret))                                                          // if we dont do something with request already
            {                                                                                       // then lets think its a command
                string[] args = message.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
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
        } // Handle steam chat message
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
        internal string FriendListShow(string[] args = null)
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
                return "Doesnt have any items to sell.";
            Log("We have " + ItemList.Count + " to sell.", LogType.Info);
            foreach (WebBot._Item Item in ItemList)
            {
                Log(await webBot.SellItem(Item).ConfigureAwait(false), LogType.Info);
            }
            return "All cards sold.";
        }
        internal async Task<string> ChangeNickname(string[] args)
        {
            string nickname = string.Join(" ", args);
            await steamFriends.SetPersonaName(nickname);
            return "Nickname changed to " + nickname;
        }
        internal async Task<string> KeyActivate(string[] args)
        {
            if (args == null || !args.Any())
                return "Empty key";
            foreach (string sKey in args) // Check all keys
            {
                if (!isKey(sKey)) { 
                    Log("Invalid key(" + sKey + ").", LogType.Info); // if key is invalid
                    continue;                                        // then skip
                }

                var callback = await KeyActivate(sKey).ConfigureAwait(true);
                if (callback == null)                                   // if can`t hadle callback
                    continue;                                           // then skip this key
                
                if (KeyResponseNeedContinue(callback,sKey))
                { 
                    uint appID = callback.Items.Any() ? callback.Items.First().Key : 0;
                    foreach (Bot bot in Bots.Values)
                    {
                        if (!(bot.isRunning && bot.steamClient.IsConnected) || bot == this) // if bot not connected or bot = this instance
                            continue;                                                       // then skip
                        if (appID != 0 && bot.AlreadyOwnedGames.Contains(appID))            // if bot already have this game
                            continue;                                                       // then skip
                        callback = await bot.KeyActivate(sKey).ConfigureAwait(false);
                        if (callback == null)                                               // if can`t hadle callback
                            continue;                                                       // then skip
                        if (!KeyResponseNeedContinue(callback,sKey))                        // if doesn`t need to continue
                            break;                                                          // then break
                    }
                }
            }
            return "Done";
        }
        internal async Task<CustomHandler.PurchaseResponseCallback> KeyActivate(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            CustomHandler.PurchaseResponseCallback callback;
            try {
                callback = await customHandler.KeyActivate(key).ConfigureAwait(false);
            } catch(Exception e) { Log("Cant hadle key activation callback. ("+e.Message+")",LogType.Error);
                return null;
            }
            return callback;
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
        internal async Task<string> UnWatchBroadcastCommand(string[] args)
        {
            if (webBot.bWatchBroadcast)
            {
                webBot.bWatchBroadcast = false;
                return "Watching broadcast was stopped";
            }
            return "Bot don watch any broadcast";

        }
        internal async Task<string> WatchBroadcastCommand(string[] args)
        {
            if (await webBot.WatchBroadcast(args[0]).ConfigureAwait(false))
                return "Start watching broadcast";
            return "Cant watch broadcast";
        }
        internal async Task<string> BuyApps(string[] args)
        {
            foreach (string subid in args)
                Log(await webBot.AddToCart(subid).ConfigureAwait(false),LogType.Info);

            return await webBot.BuyCart().ConfigureAwait(false);
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
        internal async Task<string> RefreshCMs(string[] args)
        {
            await RefreshCMs(ProgramConfig.CellID, true).ConfigureAwait(false);
            return "CM servers was refreshed";
        }
        /*
        //
        // Methods
        //
        */
        private bool KeyResponseNeedContinue(CustomHandler.PurchaseResponseCallback callback,string sKey)
        {
            bool bContinue = false;
            switch (callback.PurchaseResult)
            {
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.OnCooldown:
                    bContinue = true;
                    Log("Can`t activate key (" + sKey + "), limit reached, try later.", LogType.Info);
                    break;
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.OK:
                    Log("Key (" + sKey + ") was activate ("+callback.Items.First().Value+").", LogType.Info);
                    break;
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.AlreadyOwned:
                    Log("Game(" + callback.Items.First().Value + ") already owned.", LogType.Info);
                    if (!AlreadyOwnedGames.Contains(callback.Items.First().Key))
                        AlreadyOwnedGames.Add(callback.Items.First().Key);
                    bContinue = true;
                    break;
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.BaseGameRequired:
                    Log("Key (" + sKey + ") need base game (" + callback.Items.First().Value + ")", LogType.Info);
                    bContinue = true;
                    break;
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.DuplicatedKey:
                    Log("Key (" + sKey + ") is duplicated("+callback.Items.First().Value+").", LogType.Info);
                    break;
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.InvalidKey:
                    Log("Key (" + sKey + ") is invalid.", LogType.Info);
                    break;
                case CustomHandler.PurchaseResponseCallback.EPurchaseResult.RegionLocked:
                    Log("Can`t activate key (" + sKey + "), region locked.", LogType.Info);
                    bContinue = true;
                    break;
            }
            return bContinue;
        }
        internal bool isKey(string Key)
        {
            Match First = Regex.Match(Key, @"[0-9,a-z,A-Z]{5}-[0-9,a-z,A-Z]{5}-[0-9,a-z,A-Z]{5}");
            if (First.Success)
                return true;
            return false;
        }
        internal async Task RefreshCMs(uint _cellID, bool WithoutCache = false)
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
            if (!isManualDisconnect && BotConfig.Enabled)
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
            initialized = true;
            Status = StatusEnum.Disabled;
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
        internal async void OnWalletInfo(SteamUser.WalletInfoCallback callback)
        {
            if (callback == null)
                return;
            Wallet.HasWallet = callback.HasWallet;
            if (!Wallet.HasWallet)
                return;
            Wallet.Balance = callback.Balance;
            Wallet.Curency = callback.Currency;

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
