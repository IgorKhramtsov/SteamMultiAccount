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
    internal sealed class Bot
    {
        private const ushort CallbackSleep = 1000;

        private static bool isRunning = false;
        internal bool needAuthCode = false;
        private string authCode = string.Empty;
        private string sentryPath;

        internal static readonly Dictionary<string, Bot> Bots = new Dictionary<string, Bot>();
        internal readonly string BotName;
        internal readonly string BotPath;
        internal readonly Config BotConfig;
        internal Loging logging;
        internal readonly SteamClient steamClient;
        internal readonly SteamUser steamUser;
        internal readonly CallbackManager callbackManager;
        private string _logboxText;

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
            callbackManager = new CallbackManager(steamClient);

            BotConfig = BotConfig.Load(logging);

            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachinAuth);

            RefreshCMs(BotConfig.cellID).Wait();

            if (Bots.ContainsKey(BotName))
                return;
            Bots[BotName] = this;
            Start().Forget();
        }

        internal async void Response(string message)
        {
            if (needAuthCode && message.Length == 5)
            {
                needAuthCode = false;
                authCode = message;
                steamClient.Connect();
                return;
            }
            switch (message)
            {
                default:
                    break;
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
                SentryFileHash = sentryHash
            });
        }
        internal async void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            if (needAuthCode)
                return;
            Log("Disconnected from steam",LogType.Info);
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
