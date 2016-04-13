using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;
using System.IO;
using System.Runtime.CompilerServices;

namespace SteamMultiAccount
{
    internal sealed class Bot
    {
        private const ushort CallbackSleep = 1000;

        private static bool isRunning = false;

        internal static readonly Dictionary<string, Bot> Bots = new Dictionary<string, Bot>();
        internal readonly string BotName;
        internal readonly string BotPath;
        internal readonly Config BotConfig;
        internal Loging logging;
        internal static SteamClient steamClient;
        internal static SteamUser steamUser;
        internal readonly CallbackManager callbackManager;
        private string _logboxText;

        internal Bot(string botName)
        {
            if (string.IsNullOrEmpty(botName))
                return;
            BotName = botName;
            BotPath = Path.Combine(SMAForm.ConfigDirectory, BotName);

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

            RefreshCMs(BotConfig.cellID).Wait();

            if (Bots.ContainsKey(BotName))
                return;
            Bots[BotName] = this;

            Start().Forget();
        }

        internal void Response(string message)
        {

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
            if (string.IsNullOrEmpty(BotConfig.Login) || string.IsNullOrEmpty(BotConfig.Password))
                return;
            steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = BotConfig.Login,
                Password = BotConfig.Password
            });
        }

        internal async void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Log("Disconnected from steam",LogType.Info);
            isRunning = false;
        }

        internal async void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                if (callback.Result == EResult.AccountLogonDenied)
                {
                    Log("Unable to logon to steam account is steam guard protected",LogType.Error);
                    isRunning = false;
                    return;
                }
                Log("Unable to logon to steam (" + callback.Result + ") " + callback.ExtendedResult,LogType.Error);
                isRunning = false;
                return;
            }
            Log("Successfully logged on!",LogType.Info);

        }

        internal async void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Log("Logged off of steam (" + callback.Result + ")",LogType.Info);
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
            Log("Connecting to steam...",LogType.Info);
        steamClient.Connect();
    }
    private void CallbacksHandler()
    {
        while (isRunning && steamClient.IsConnected)
        {
            callbackManager.RunWaitCallbacks(TimeSpan.FromMilliseconds(CallbackSleep));
        }
    }
    internal async Task RefreshCMs(uint cellID)
        {
            if (File.Exists(SMAForm.ServerLists) && DateTime.Now.Subtract( File.GetLastWriteTime(SMAForm.ServerLists)) < TimeSpan.FromMinutes(Config.ServerFileLifeTime))
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
                    Log("Refreshing list of CM servers",LogType.Info);
                    await SteamDirectory.Initialize(cellID).ConfigureAwait(false);
                    Config.ServerListSave(CMClient.Servers);
                    initialized = true;
                } catch(Exception e) {
                    Log("Cant refresh list of CMs "+e.Message,LogType.Error);
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
