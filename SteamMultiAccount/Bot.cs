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
using System.Windows.Forms;
using System.Diagnostics;
using HSteamUser = Steamworks.HSteamUser;

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
        internal static readonly uint loginID = MsgClientLogon.ObfuscationMask;
        internal static readonly Dictionary<string, Bot> Bots = new Dictionary<string, Bot>();
        internal static readonly string[] CommandsKeys = { "Nickname", "Sellcards", "Watch", "Unwatch", "Buy" , "PlayDota"};

        /* Bot flags */
        internal                 bool isRunning, initialized, needAuthCode, needTwoFactorAuthCode, Restarting;
        private                  bool isManualDisconnect;
        /* Temp strings */
        private                  string authCode, twoFactorAuthCode;
        /* Service stuff */
        private         readonly SMAForm InitializerForm;
        internal                 System.Threading.Timer timer;
        internal                 delegate Task<string> MyDelegate(string[] args);
        /* Lists */
        internal                 List<uint> AlreadyOwnedGames;
        internal                 List<Game> CurrentFarming;
        internal        readonly Dictionary<string, MyDelegate> Commands;
        internal        readonly List<SteamID> FriendList = new List<SteamID>();
        internal                 StatusEnum Status;
        internal                 sWallet Wallet;
        /* Environment variables*/
        internal        readonly string BotName;
        internal        readonly string BotPath;
        /* Classes */
        internal static          ProgramConfig ProgramConfig;
        internal                 BotConfig BotConfig;
        internal        readonly Logging logging;
        internal        readonly SteamClient steamClient;
        internal        readonly SteamUser steamUser;
        internal        readonly SteamFriends steamFriends;
        internal        readonly SteamApps steamApps;
        internal        readonly CallbackManager callbackManager;
        internal        readonly WebBot webBot;
        internal        readonly DotaBot dotaBot;

        internal        readonly CustomHandler customHandler;
        

        internal Bot(string botName,SMAForm initializer)
        {
            if (string.IsNullOrEmpty(botName))
                return;
            if (initializer == null)
                return;
            InitializerForm = initializer;
            BotName = botName;
            BotPath = Path.Combine(SMAForm.ConfigDirectory, BotName);
            
            AlreadyOwnedGames = new List<uint>();
            CurrentFarming = new List<Game>();
            Commands = new Dictionary<string, MyDelegate>()
            {
                {CommandsKeys[0], ChangeNickname},
                {CommandsKeys[1], Sellcards},
                {CommandsKeys[2], WatchBroadcastCommand},
                {CommandsKeys[3], UnWatchBroadcastCommand},
                {CommandsKeys[4], BuyApps},
                {CommandsKeys[5], PlayDota},
            };

            logging = new Logging();
            BotConfig = new BotConfig(BotPath);
            steamClient = new SteamClient();
            steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();
            steamApps = steamClient.GetHandler<SteamApps>();
            callbackManager = new CallbackManager(steamClient);
            webBot = new WebBot();
            dotaBot = new DotaBot(logging,steamClient,callbackManager);
            customHandler = new CustomHandler();

            BotConfig = BotConfig.Load();
            timer = new System.Threading.Timer(e => Farm());

            steamClient.AddHandler(customHandler);
            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachinAuth);
            callbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnLoginKey);
            callbackManager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);
            callbackManager.Subscribe<SteamUser.WalletInfoCallback>(OnWalletInfo);

            //callbackManager.Subscribe<SteamFriends.FriendMsgCallback>(OnFriendMessage); Dont handle messages
            callbackManager.Subscribe<SteamFriends.FriendsListCallback>(OnFriendList);

            callbackManager.Subscribe<SteamApps.GuestPassListCallback>(OnGuestPass);
            callbackManager.Subscribe<SteamApps.FreeLicenseCallback>(OnFreeLicense);

            // TODO: Join to game aways on sites

            if (Bots.ContainsKey(BotName))
                return;
            Bots[BotName] = this;
            if (ProgramConfig == null)
            {
                ProgramConfig = new ProgramConfig();
                ProgramConfig = ProgramConfig.Load();
            }
            SteamClient.Servers.CellID = BotConfig.CellID;
            SteamClient.Servers.ServerListProvider = new SteamKit2.Discovery.FileStorageServerListProvider(SMAForm.ServerList);
            NetDebug();

            if (BotConfig.Enabled)
                Start().Forget();
            else
            {
                Status = StatusEnum.Disabled;
                initialized = true;
            }
            
        }

        internal async Task Start()
        {
            if (isRunning && steamClient.IsConnected)
            {
                Status = StatusEnum.Connected;
                return;
            }
            initialized = false;

            var config = BotConfig.Load();
            if (config != BotConfig)
                BotConfig = config;

            isRunning = true;
            Task.Run(() => CallbacksHandler()).Forget();
            Log("Connecting to steam...", LogType.Info);
            Status = StatusEnum.Connecting;
            steamClient.Connect();
        }
        internal async Task Stop()
        {
            isManualDisconnect = true;
            initialized = false;
            Log("Disconnecting from steam...", LogType.Info);
            if (steamUser.SteamID != null)
                steamUser.LogOff();
            steamClient.Disconnect();
            /* Farming cleaning */
            foreach (var game in CurrentFarming)
                game.StopIdle();
            CurrentFarming.Clear();
            FarmTimerStop();
        }
        internal void Restart()
        {
            if (steamClient.IsConnected)
            {
                Restarting = true;
                Stop().Forget();
            }
            else
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
                if (isKey(keys.First()))//if first element is key, then lets think all elements are keys
                {
                    var res = await KeyActivate(keys).ConfigureAwait(false);
                    InitializerForm.Invoke(new MethodInvoker(delegate { KeyManager keyManager = new KeyManager(res); keyManager.Show(); }));
                    Ret = "Keys accepted";
                }
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
        internal void FarmGame(List<Game> Games)
        {
            /* Prepeare CurrentFarming list */
            foreach (var game in Games)
                if (!CurrentFarming.Contains(game))
                    CurrentFarming.Add(game);
            var listGames = new List<Game>(CurrentFarming);
            foreach (var game in listGames)
                if (!Games.Contains(game))
                {
                    game.StopIdle();
                    CurrentFarming.Remove(game);
                }
            /*If user authorized in steam client*/
            if (Steamworks.SteamAPI.IsSteamRunning() && BotConfig.IsAuthorizedInSteamClient)
            {
                int i = 0;
                listGames = new List<Game>(CurrentFarming);
                foreach (var game in listGames)
                {
                    if (ProgramConfig.SimultaneousGamesFarming > 0 && i >= ProgramConfig.SimultaneousGamesFarming) // Delete remaining 
                    {
                        game.StopIdle();
                        CurrentFarming.Remove(game);
                        continue;
                    }
                    game.StartIdle();
                    i++;
                }
                return;
            }
            /* If user not authorized in steam client */
            /* To stop farming we must send request without games */
            var req = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
            foreach (var game in CurrentFarming)
            {
                req.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed() { game_id = new GameID(game.appID) });
            }
            steamClient.Send(req);
        }
        internal void FarmGame(Game game)
        {
            FarmGame(new List<Game>() { game });
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
        internal async Task<List<Key>> KeyActivate(string[] args)
        {
            /*
             * 1)   Check if key is valid
             * 2)   Create list of keys
             * 3)   Try activate key on current bot
             * 4.a) If cooldown -> return error
             * 4.b) If key activated -> add it to list, next key
             * 4.c) If key is invalid -> next key
             * 4.d) If game already owned -> add it to list, go to 5)
             * 5)   Get all bots and try to activate on them
             * 6)   If this bot already have this game -> next bot
             * 7.a) If cooldown -> next bot
             * 7.b) If key activated -> add it to list, next key
             * 7.c) If key is invalid -> next key
             * 7.d) If game already owned -> add it to list, go to next bot
             */
            if (args == null || !args.Any())
                return new List<Key>();

            var Keys = new List<Key>();
            foreach(var _key in args)
            {
                if (!isKey(_key))
                    continue;
                var key = new Key(_key);
                if (!Keys.Contains(key))
                    Keys.Add(key);
            }
            if (!Keys.Any())
                return Keys;

            foreach(var Key in Keys)
            {
                CustomHandler.PurchaseResponseCallback res = null;
                for (byte i = 0; i < 3 && res == null ; i++)
                {
                    res = await this.KeyActivate(Key.key);
                    if (res.PurchaseResult == CustomHandler.PurchaseResponseCallback.EPurchaseResult.Unknown)
                        res = null;
                }
                if (res == null)
                    continue;

                Key.ActivatingResult = res.PurchaseResult;
                switch(res.PurchaseResult)
                {
                    case CustomHandler.PurchaseResponseCallback.EPurchaseResult.OnCooldown:
                        Key.botName = BotName;
                        return Keys;
                    case CustomHandler.PurchaseResponseCallback.EPurchaseResult.OK:
                        Key.appID = res.Items.First().Key;
                        Key.gameName = res.Items.First().Value;
                        this.AlreadyOwnedGames.Add(Key.appID);
                        Key.botName = this.BotName;
                        continue;
                    case CustomHandler.PurchaseResponseCallback.EPurchaseResult.AlreadyOwned:
                        Key.appID = res.Items.First().Key;
                        Key.gameName = res.Items.First().Value;
                        this.AlreadyOwnedGames.Add(Key.appID);
                        break;
                    case CustomHandler.PurchaseResponseCallback.EPurchaseResult.BaseGameRequired:
                    case CustomHandler.PurchaseResponseCallback.EPurchaseResult.RegionLocked:
                        break;
                    case CustomHandler.PurchaseResponseCallback.EPurchaseResult.InvalidKey:
                    case CustomHandler.PurchaseResponseCallback.EPurchaseResult.DuplicatedKey:
                    case CustomHandler.PurchaseResponseCallback.EPurchaseResult.Unknown:
                        continue;
                }
                foreach(var bot in Bots.Values)
                {
                    bool stop = false;
                    if (bot.AlreadyOwnedGames.Contains(Key.appID))
                        continue;

                    res = null;
                    for (byte i = 0; i < 3 && res == null; i++)
                    {
                        res = await this.KeyActivate(Key.key);
                        if (res.PurchaseResult == CustomHandler.PurchaseResponseCallback.EPurchaseResult.Unknown)
                            res = null;
                    }
                    if (res == null)
                        continue;

                    Key.ActivatingResult = res.PurchaseResult;
                    switch (res.PurchaseResult)
                    {
                        case CustomHandler.PurchaseResponseCallback.EPurchaseResult.OnCooldown:
                            continue;
                        case CustomHandler.PurchaseResponseCallback.EPurchaseResult.OK:
                            Key.appID = res.Items.First().Key;
                            Key.gameName = res.Items.First().Value;
                            bot.AlreadyOwnedGames.Add(Key.appID);
                            Key.botName = this.BotName;
                            stop = true;
                            break;
                        case CustomHandler.PurchaseResponseCallback.EPurchaseResult.AlreadyOwned:
                            Key.appID = res.Items.First().Key;
                            Key.gameName = res.Items.First().Value;
                            bot.AlreadyOwnedGames.Add(Key.appID);
                            continue;
                        case CustomHandler.PurchaseResponseCallback.EPurchaseResult.BaseGameRequired:
                        case CustomHandler.PurchaseResponseCallback.EPurchaseResult.RegionLocked:
                            continue;
                        case CustomHandler.PurchaseResponseCallback.EPurchaseResult.InvalidKey:
                        case CustomHandler.PurchaseResponseCallback.EPurchaseResult.DuplicatedKey:
                        case CustomHandler.PurchaseResponseCallback.EPurchaseResult.Unknown:
                            stop = true;
                            break;
                    }
                    if (stop)
                        break;
                }
            }
            return Keys;
        } // Key activate algorithm
        internal async Task<CustomHandler.PurchaseResponseCallback> KeyActivate(string key) // Key activate method
        {
            if (string.IsNullOrWhiteSpace(key))
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

            if (!(webBot.GamesToFarmSolo.Any() || webBot.GamesToFarmMulti.Any())) // If we dont have anything to farm
            { 
                Status = StatusEnum.Connected;
                FarmGame(new List<Game>());
                timer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMinutes(15));
                Status = StatusEnum.Connected;
                return;
            }

            if (webBot.GamesToFarmMulti.Count > 1)
            {
                Status = StatusEnum.Farming;
                FarmGame(webBot.GamesToFarmMulti); // Farm multi if we have more than 1 game without ability to farm cards
            }
            else
            {
                if (webBot.GamesToFarmMulti.Any())
                    webBot.GamesToFarmSolo.Add(webBot.GamesToFarmMulti.First()); // Else add our list to farm multi to solo farm list
                Status = StatusEnum.Farming;
                FarmGame(webBot.GamesToFarmSolo.First()); // And farm first game from list
            }
        } // Cards farming algorithm
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

            
            if (Status == StatusEnum.Farming)
            {
                FarmTimerStop();
                FarmGame(new List<Game>());
                Status = StatusEnum.Connected;
            }
            else
                FarmTimerStart();
        }
        internal void PauseResume()
        {
            if (steamClient.IsConnected && isRunning)
                Stop().Forget();
            else
                Start().Forget();
        }
        internal async Task RefreshSessionIfNeeded()
        {
            if (!webBot.Initialized)
                return;
            if (await webBot.IsLoggedIn().ConfigureAwait(false))
                return;

            SteamUser.WebAPIUserNonceCallback callback = null;
            for (byte i = 0; i < 3 && (callback == null || callback.Result != EResult.OK); i++)
                callback = await steamUser.RequestWebAPIUserNonce();
            if (callback == null)
                Log("Cant get webAPI user nonce: " + callback.Result,LogType.Error);

            webBot.Init(this, callback.Nonce).Forget();
        }
        internal async Task<string> PlayDota(string[] args)
        {
            Log("Launching DOTA 2...", LogType.Info);
            await dotaBot.Initialize().ConfigureAwait(false);
            /*
            FarmGame(new Game(DotaBot.appID, "Dota 2")); // Running dota 2.
            await Task.Delay(5000); // Wait until game coordinator be ready.
            var clientHello = new SteamKit2.GC.ClientGCMsgProtobuf<SteamKit2.GC.Dota.Internal.CMsgClientHello>((uint)SteamKit2.GC.Dota.Internal.EGCBaseClientMsg.k_EMsgGCClientHello);
            clientHello.Body.engine = SteamKit2.GC.Dota.Internal.ESourceEngine.k_ESE_Source2;
            dotaCoordinator.Send(clientHello,DotaBot.appID);
            */

            return "";
        }
        /*
        //
        // Methods
        //
        */
        internal bool isKey(string Key)
        {
            Match First = Regex.Match(Key, @"[0-9,a-z,A-Z]{5}-[0-9,a-z,A-Z]{5}-[0-9,a-z,A-Z]{5}");
            if (First.Success)
                return true;
            return false;
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

            string sentryPath = Path.Combine(SMAForm.BotsData, BotName + ".bin");
            byte[] sentryHash = null;
            if (File.Exists(sentryPath))
            {
                // if we have a saved sentry file, read and sha-1 hash it
                byte[] sentryFile = File.ReadAllBytes(sentryPath);
                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }
            if (string.IsNullOrWhiteSpace(BotConfig.Login) || string.IsNullOrWhiteSpace(BotConfig.Password))
            {
                Log("Login or password doesnt entered.", LogType.Warning);
                steamClient.Disconnect();
                return;
            }

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
            if (Restarting)
            {
                Restarting = false;
                Thread.Sleep(5000);
                Start().Forget();
                return;
            }
            Log("Disconnected from steam.",LogType.Info);
            if (!isManualDisconnect)
            {
                Log("Reconnecting to steam", LogType.Info);
                Thread.Sleep(5000);
                Start().Forget();
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
                    Log("Auth code was sended to your email.", LogType.Warning);
                    Log("Please type your auth code below.", LogType.Warning);
                    needAuthCode = true;
                    return;
                }
                else if (callback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
                {
                    Log("Please type your two factor auth code below.", LogType.Warning);
                    needTwoFactorAuthCode = true;
                    return;
                }
                else if (callback.Result == EResult.InvalidPassword)
                {
                    if (!string.IsNullOrWhiteSpace(BotConfig.loginKey))
                    {
                        BotConfig.loginKey = null;
                        BotConfig.Save();
                        return;
                    }
                    Log("Invalid password.", LogType.Warning);
                }
                else if (callback.Result == EResult.NoConnection)
                    Log("No connection. Try again later.", LogType.Warning);
                else
                    Log("Unable to logon to steam (" + callback.Result + ") " + callback.ExtendedResult, LogType.Error);

                isRunning = false;
                initialized = true;
                Status = StatusEnum.Disabled;

                return;
            }
            BotConfig.SetCellID(callback.CellID);
            Log("Successfully logged on!",LogType.Info);
            Status = StatusEnum.Connected;

            await webBot.Init(this, callback.WebAPIUserNonce).ConfigureAwait(true);
            if (webBot.Initialized)
                AfterWebInit();
            else
                initialized = true;

            if (BotConfig.AutoFarm)
                FarmTimerStart();
        }
        internal async void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Log("Logged off of steam (" + callback.Result + ")", LogType.Info);
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
            if (!BotConfig.FarmOffline)
                await steamFriends.SetPersonaState(EPersonaState.Online);
        }
        internal async void OnFriendMessage(SteamFriends.FriendMsgCallback callback)
        {
            if (callback.EntryType == EChatEntryType.ChatMsg)
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
                    Log(steamFriends.GetFriendPersonaName(friend.SteamID)+" was added to friends list",LogType.Info);
                }
                /*
                if (friend.SteamID.IsIndividualAccount) // Friend list
                    FriendList.Add(friend.SteamID);
                */
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
                else
                    Log("Fail", LogType.Info);
            }
            if (AcceptedSomething)
                Farm();
        }
        internal async void OnWalletInfo(SteamUser.WalletInfoCallback callback)
        {
            if (callback == null)
                return;
            Wallet.HasWallet = callback.HasWallet;
            if (Wallet.HasWallet)
            {
                Wallet.Balance = callback.Balance;
                Wallet.Curency = callback.Currency;
            }

        }
        internal async void AfterWebInit()
        {
            initialized = true;
        }
        /*
        //
        // Services
        //
        */
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
        private void FarmTimerStop()
        {
            timer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMinutes(15));
        }
        private void FarmTimerStart()
        {
            timer.Change(TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(15));
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
        internal void Log(string message, LogType type = LogType.Info, [CallerMemberName] string functionName = "")
        {
            logging.Log(message, type, functionName);
        }
        internal string getLogBoxText()
        {
            return logging.GetLogBoxText();
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
