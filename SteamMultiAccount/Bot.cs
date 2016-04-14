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

namespace SteamMultiAccount
{
    enum eCommands
    {
        nickname
    }
    internal sealed class Bot
    {
        private const ushort CallbackSleep = 1000;

        internal bool isRunning = false;
        internal bool needAuthCode = false;
        private bool isManualDisconnect = false;
        private string authCode = string.Empty;
        private readonly string sentryPath;
        private string _logboxText;
        internal static readonly string[] Commands = new string[3] {"nickname ","FriendList","send "};

        internal static uint cellID = 0;
        internal static readonly uint loginID = MsgClientLogon.ObfuscationMask;
        internal static readonly Dictionary<string, Bot> Bots = new Dictionary<string, Bot>();
        internal        readonly List<SteamID> FriendList = new List<SteamID>();
        internal readonly string BotName;
        internal readonly string BotPath;
        internal readonly Config BotConfig;
        internal readonly Loging logging;
        internal readonly SteamClient steamClient;
        internal readonly SteamUser steamUser;
        internal readonly SteamFriends steamFriends;
        internal readonly CallbackManager callbackManager;

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

            BotConfig = BotConfig.Load(logging);

            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachinAuth);
            callbackManager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);

            callbackManager.Subscribe<SteamFriends.FriendMsgCallback>(OnFriendMessage);
            callbackManager.Subscribe<SteamFriends.FriendsListCallback>(OnFriendList);

            NetDebug();

            if (cellID == 0 && BotConfig.cellID != 0)
                cellID = BotConfig.cellID;

            RefreshCMs(cellID).Wait();

            if (Bots.ContainsKey(BotName))
                return;
            Bots[BotName] = this;
            if(BotConfig.Enabled)
            Start().Forget();
        }

        internal async Task<string> Response(string message)
        {
            if (string.IsNullOrEmpty(message))
                return null;
            string Ret = string.Empty;
            if (needAuthCode && message.Length == 5)
            {
                needAuthCode = false;
                authCode = message;
                steamClient.Connect();
                return null;
            }
            if (message.Contains(" "))
            {
                string[] args = message.Split(' ');
                switch (args[0])
                {
                    case "nickname":
                        await steamFriends.SetPersonaName(message.Substring("nickname ".Length));
                        Ret = ("Nickname changed to \"" + message.Substring("nickname ".Length) + "\"");
                        break;
                    case "send":
                        UInt64 steamid;
                        UInt64.TryParse(args[1], out steamid);
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
        internal string FriendListShow()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var friend in FriendList)
            {
                sb.Append(Environment.NewLine+ "[" + friend.ConvertToUInt64() + "] " + steamFriends.GetFriendPersonaName(friend));
            }
            return sb.ToString();
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
                LoginID = loginID,
                SentryFileHash = sentryHash
            });
        }
        internal async void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            if (needAuthCode)
                return;
            Log("Disconnected from steam",LogType.Info);
            if (!isManualDisconnect)
                steamClient.Connect();
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
                Log("Unable to logon to steam (" + callback.Result + ") " + callback.ExtendedResult,LogType.Error);
                isRunning = false;
                return;
            }
            BotConfig.SetCellId(callback.CellID);
            Log("Successfully logged on!",LogType.Info);
        }
        internal async void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
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
