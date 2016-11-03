using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using SteamKit2;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Windows.Forms;

namespace SteamMultiAccount
{
    internal class GiveawayBot
    {
        internal WebClient WebClient;
        internal BotConfig Config;
        internal CookieCollection Cookie = new CookieCollection();
        internal GiveawayForm form;
        internal GiveawayBot(WebClient webClient, BotConfig config)
        {
            if (webClient == null || config == null)
            {
                Logging.LogToFile("WebClient is null");
                return;
            }
            this.WebClient = webClient;
            this.Config = config;
        }

        internal virtual string MainURL { get; set; } = "";
        internal virtual string LoginURL { get; set; } = "";
        internal virtual string GiveawayParseURL { get; set; } = "";
        internal virtual string Host { get; set; } = "";
        internal virtual string isLoggedInMark { get; set; } = "";
        internal virtual string PointsMark { get; set; } = "";
        internal virtual string LevelMark { get; set; } = "";

        internal virtual int Points { get; set; }
        internal virtual int Level { get; set; }
        internal bool isRunning { get; set; }
        
        internal string status { get; set; }
        private string _log;
        internal string Log { get { return _log; } set { _log += value + "\n"; StatusUpdate(); } }
        internal virtual async Task<bool> StartStop(GiveawayForm form = null )
        {
            if (isRunning)
                Stop();
            else
                return await Run(form == null ? this.form : form);
            return true;
        }
        internal virtual void Stop()
        {
            this.isRunning = false;
            this.status = "";
            StatusUpdate();
        }
        internal virtual void StatusUpdate()
        {
            if (form == null)
                return;
            this.status = $"Running \n Points: {Points} " + (Level == 0 ? "" : $"\n Level: {Level}");
            form.Invoke(new MethodInvoker(delegate
            {
                form.UpdateStatus(status, string.IsNullOrWhiteSpace(Log) ? "" : Log);
            }));
                //form.Invoke(new MethodInvoker(delegate { form.UpdateStatus(string.Empty); }));
        }
        internal virtual async Task<bool> Run(GiveawayForm form)
        {
            if (form == null)
                return false;
            this.form = form;

            isRunning = await this.Login();
            while (isRunning)
            {
                StatusUpdate();
                if (Points > 0)
                    await CheckGiveaways().ConfigureAwait(false);
                await Task.Delay(15 * 60 * 1000); // Wait 15min
                await RefreshProfile();
            }

            return true;
        }
        internal virtual async Task CheckGiveaways()
        {
            isRunning = true;
            int LastPage = 0;
            int Page = 1;
            do
            {
                if (!isRunning)
                    return;

                var Data = new Dictionary<string, string>()
                {
                    {"page", Page.ToString()},
                    {"count", "10"},
                    {"type", "any"},
                    {"enter_price", "on"},
                    {"sortby", "finish"},
                    {"order", "asc"},
                    {"filter_entered", "on"}
                };
                JObject response = null;
                for (byte i = 0; i < 3 && response == null; i++)
                    response = await WebClient.GetJObject(new Uri(GiveawayParseURL), Data, cookies: Cookie).ConfigureAwait(false);
                if (response == null)
                    continue;
                List<Task> tasks = new List<Task>();
                foreach (var giveaway in response["giveaways"])
                {
                    var code = giveaway["code"].Value<string>();
                    var name = giveaway["game"]["name"].Value<string>();
                    if (string.IsNullOrWhiteSpace(code))
                        continue;
                    tasks.Add(EnterGiveaway(code, name));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
                if (LastPage == 0)
                    LastPage = response["last_page"].Value<int>();
            } while (Page <= LastPage);
        }

        internal async Task<bool> Login()
        {
            // TODO: Use Proxy
            /* If instances of bot is more than 1, use proxy for all other instance
             * Becouse GameMiner(and maybe othe giveaway sites) can ban account with same ip 
             */
            if (await isLoggedIn().ConfigureAwait(false))
                return true;

            Log = "Logging in";
            var response = await WebClient.GetDocument(new Uri(LoginURL)).ConfigureAwait(false);
            if (response == null)
                return false;

            var nParams = response.DocumentNode.SelectSingleNode("//input[@name='openidparams']");
            var nNonce = response.DocumentNode.SelectSingleNode("//input[@name='nonce']");
            var Data = new Dictionary<string, string>()
            {
                {"action", "steam_openid_login"},
                {"openid.mode", "checkid_setup"},
                {"openidparams", nParams.GetAttributeValue("value","")},
                {"nonce", nNonce.GetAttributeValue("value","")}
            };
            var Hresp = await WebClient.GetContent(new Uri("https://steamcommunity.com/openid/login"), Data, HttpMethod.Post, null, false);
            if (Hresp == null)
                return false;
            var url = Hresp.Headers.Location;
            Hresp = null;
            Hresp = await WebClient.GetContent(url, bAutoRedir: false);
            if (Hresp == null)
                return false;
            IEnumerable<string> cookies;
            if (!Hresp.Headers.TryGetValues("set-cookie",out cookies))
                return false;
            AddCookies(cookies.ToList<string>());

            Log = "Successfull logged in.";
            return await RefreshProfile().ConfigureAwait(false);
        }
        internal virtual async Task EnterGiveaway(string code, string name = "")
        {

        }
        internal virtual async Task<bool> RefreshProfile()
        {
            Log = "Refreshing profile";
            var resp = await WebClient.GetContent(new Uri(MainURL)).ConfigureAwait(false);
            if (resp == null)
                return false;
            IEnumerable<string> cookies;
            if (resp.Headers.TryGetValues("set-cookie", out cookies))
                AddCookies(cookies.ToList<string>());
            var doc = await WebClient.GetDocument(resp).ConfigureAwait(false);
            if (doc == null)
                return false;

            var nPoints = doc.DocumentNode.SelectSingleNode(PointsMark);
            var nLevel = doc.DocumentNode.SelectSingleNode(LevelMark);

            if (nPoints == null || nLevel == null)
                return false;

            Points = int.Parse(nPoints.InnerText);
            Level = int.Parse(nLevel.InnerText);

            Log = "Successfull";
            return true;
        }
        internal async Task<bool> isLoggedIn()
        {
            var response = await WebClient.GetContent(new Uri(MainURL)).ConfigureAwait(false);
            if (response == null)
                return false;

            IEnumerable<string> cookies;
            if (!response.Headers.TryGetValues("set-cookie", out cookies))
                return false;
            AddCookies(cookies.ToList<string>());
            
            var responseDoc = await WebClient.GetDocument(response).ConfigureAwait(false);
            //var login = response.DocumentNode.SelectSingleNode(isLoggedInMark/*"//a[@class='nav__sits']"*/);
            var login = responseDoc.DocumentNode.SelectSingleNode(isLoggedInMark);
            return (login == null);
        }
        internal void AddCookies(List<string> cookies)
        {
            if (cookies == null)
                return;
            foreach (var cookie in cookies)
            {
                string name = cookie.Substring(0, cookie.IndexOf("="));
                string val = cookie.Substring(cookie.IndexOf("=") + 1, cookie.IndexOf(";") - cookie.IndexOf("=") - 1);
                Cookie.Add(new Cookie(name, val, "/", "." + Host));
            }
        }
        private GiveawayBot() { }
    }
    internal sealed class GameminerBot : GiveawayBot
    {
        const string GameMinerURL = "http://gameminer.net/";
        const string GameMinerHost = "gameminer.net";
        const string GameMinerAPI = GameMinerURL + "api/giveaways/";
        const string GameMinerWon = GameMinerURL + "giveaways/won/";
        const string GameMinerCoal = GameMinerAPI + "coal";
        const string GameMinerGold = GameMinerAPI + "gold";
        const string GameMinerEnter = GameMinerURL + "giveaway/enter/";

        public const string Name = "Gameminer";
        internal GameminerBot(WebClient client, BotConfig config) : base(client, config)
        {
            MainURL = "http://gameminer.net/";
            LoginURL = MainURL + "login/steam?backurl=http%3A%2F%2Fgameminer.net%2F&agree=True";
            Host = "gameminer.net";
            isLoggedInMark = "//a[@class='enter-steam']";
            PointsMark = "//span[@class='user__coal']";
            LevelMark = "//span[@class='g-level-icon']";
        }
        public override string ToString()
        {
            return Name;
        }
        /*
        internal async Task<bool> RefreshProfile()
        {
            var resp = await WebClient.GetContent(new Uri(GameMinerURL)).ConfigureAwait(false);
            if (resp == null)
                return false;
            if (!string.IsNullOrWhiteSpace(resp.Headers.GetValues("set-cookie").ToArray()[0].ToString()))
            {
                var cookies = resp.Headers.GetValues("set-cookie").ToList();
                foreach (var cookie in cookies)
                {
                    if (!cookie.Contains("_xsrf"))
                        continue;
                    string val = cookie.Substring("_xsrf=".Length);
                    val = val.Substring(0, val.IndexOf(";"));
                    xsrf = val;
                    Cookie.Add(new Cookie("_xsrf", val, "/", "." + GameMinerHost));
                }
            }
            var doc = await WebClient.GetDocument(resp).ConfigureAwait(false);
            if (doc == null)
                return false;

            var nCoal = doc.DocumentNode.SelectSingleNode("//span[@class='user__coal']");
            var nLevel = doc.DocumentNode.SelectSingleNode("//span[@class='g-level-icon']");
            var nUsername = doc.DocumentNode.SelectSingleNode("//a[@class='dashboard__user-name']");

            if (nCoal == null || nLevel == null || nUsername == null)
                return false;

            iCoal = int.Parse(nCoal.InnerText);
            iLevel = int.Parse(nLevel.InnerText);
            sUsername = nUsername.InnerText;
            return true;
        }
        */
        internal override async Task CheckGiveaways()
        {
            isRunning = true;
            int LastPage = 0;
            int Page = 1;
            do
            {
                if (!isRunning)
                    return;

                var Data = new Dictionary<string, string>()
                {
                    {"page", Page.ToString()},
                    {"count", "10"},
                    {"type", "any"},
                    {"enter_price", "on"},
                    {"sortby", "finish"},
                    {"order", "asc"},
                    {"filter_entered", "on"}
                };
                JObject response = null;
                for (byte i = 0; i < 3 && response == null; i++)
                    response = await WebClient.GetJObject(new Uri(GameMinerCoal), Data, cookies: Cookie).ConfigureAwait(false);
                if (response == null)
                    continue;
                List<Task> tasks = new List<Task>();
                foreach (var giveaway in response["giveaways"])
                {
                    var code = giveaway["code"].Value<string>();
                    var name = giveaway["game"]["name"].Value<string>();
                    if (string.IsNullOrWhiteSpace(code))
                        continue;
                    tasks.Add(EnterGiveaway(code, name));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
                if (LastPage == 0)
                    LastPage = response["last_page"].Value<int>();
            } while (Page <= LastPage);

            return;
        }
        internal override async Task EnterGiveaway(string code, string name = "")
        {
            if (!isRunning)
                return;
            await Task.Delay(new Random().Next(1, 5) * 1000).ConfigureAwait(false); // Random delay 1-5 sec
            var data = new Dictionary<string, string>()
            {
                { "_xsrf", Cookie["_xsrf"].Value },
                { "json", "true" }
            };

            var response = await WebClient.GetJObject(new Uri(GameMinerEnter + code), data, HttpMethod.Post, Cookie, new Uri(MainURL));
            if (response == null)
                return;
            if (response["status"].Value<string>() != "ok")
            {
                Log = $"Cant enter to giveaway \"{name}\"";
                Logging.LogToFile("Cant enter to giveaway: " + response.ToString());
                return;
            }
            Points = response["coal"].Value<int>();
            Log = $"Entered to \"{name}\"";
            return;
        }
        /*
        internal async Task<bool> Login()
        {
            // TODO: Use Proxy
            // Create static field, if it more than 1, use proxy for all other bots
            // Becouse GameMiner(and maybe othe giveaway sites) can ban account with same ip 

            string loginURL = GameMinerURL + "login/steam?backurl=http%3A%2F%2F" + GameMinerHost + "%2F&agree=True";
            var response = await WebClient.GetDocument(new Uri(loginURL)).ConfigureAwait(false);
            if (response == null)
                return false;

            var nParams = response.DocumentNode.SelectSingleNode("//input[@name='openidparams']");
            var nNonce = response.DocumentNode.SelectSingleNode("//input[@name='nonce']");
            var Data = new Dictionary<string, string>()
            {
                {"action", "steam_openid_login"},
                {"openid.mode", "checkid_setup"},
                {"openidparams", nParams.GetAttributeValue("value","")},
                {"nonce", nNonce.GetAttributeValue("value","")}
            };
            var Hresp = await WebClient.GetContent(new Uri("https://steamcommunity.com/openid/login"), Data, HttpMethod.Post, null, false);
            if (Hresp == null)
                return false;
            var url = Hresp.Headers.Location;
            Hresp = null;
            Hresp = await WebClient.GetContent(url, bAutoRedir: false);
            if (Hresp == null)
                return false;
            if (string.IsNullOrWhiteSpace(Hresp.Headers.GetValues("set-cookie").ToArray()[0]))
                return false;
            string token = Hresp.Headers.GetValues("set-cookie").ToList().First();
            token = token.Substring("token=".Length);
            token = token.Substring(0, token.IndexOf(";"));
            Cookie.Add(new Cookie("token", token, "/", "." + GameMinerHost));

            return await RefreshProfile().ConfigureAwait(false);
        }
        */
    }
    internal class SteamGiftsBot : GiveawayBot
    {
        const string SteamGiftsURL = "https://www.steamgifts.com/";
        const string SteamGiftsHost = "steamgifts.com";
        const string SteamGiftsAjax = SteamGiftsURL + "ajax.php";
        const string SteamGiftsWon = SteamGiftsURL + "giveaways/won";
        const string SteamGiftsSearch = SteamGiftsURL + "giveaways/search";

        public const string Name = "SteamGifts";

        internal int Points { get; private set; }
        internal int Level { get; private set; }

        internal SteamGiftsBot(WebClient client, BotConfig config) : base(client,config)
        {
            isLoggedInMark = "//a[@class='nav__sits']";
        }
        public override string ToString()
        {
            return Name;
        }
        internal async Task<bool> Login()
        {
            string loginURL = SteamGiftsURL + "?login";
            var response = await WebClient.GetDocument(new Uri(loginURL)).ConfigureAwait(false);
            if (response == null)
                return false;
            var nParams = response.DocumentNode.SelectSingleNode("//input[@name='openidparams']");
            var nNonce = response.DocumentNode.SelectSingleNode("//input[@name='nonce']");
            var Data = new Dictionary<string, string>()
            {
                {"action", "steam_openid_login"},
                {"openid.mode", "checkid_setup"},
                {"openidparams", nParams.GetAttributeValue("value","")},
                {"nonce", nNonce.GetAttributeValue("value","")}
            };
            var Hresp = await WebClient.GetContent(new Uri("https://steamcommunity.com/openid/login"), Data, HttpMethod.Post, null, false);
            if (Hresp == null)
                return false;
            var url = Hresp.Headers.Location;
            Hresp = null;
            Hresp = await WebClient.GetContent(url, bAutoRedir: false);
            if (Hresp == null)
                return false;
            if (string.IsNullOrWhiteSpace(Hresp.Headers.GetValues("set-cookie").ToArray()[0]))
                return false;
            string token = Hresp.Headers.GetValues("set-cookie").ToList().First();
            token = token.Substring("PHPSESSID=".Length);
            token = token.Substring(0, token.IndexOf(";"));
            Cookie.Add(new Cookie("PHPSESSID", token, "/", "." + SteamGiftsHost));
            return await RefreshProfile().ConfigureAwait(false);
        }
        internal async Task<bool> RefreshProfile()
        {
            var resp = await WebClient.GetContent(new Uri(SteamGiftsURL)).ConfigureAwait(false);
            if (resp == null)
                return false;
            var doc = await WebClient.GetDocument(resp).ConfigureAwait(false);
            if (doc == null)
                return false;

            var nCoal = doc.DocumentNode.SelectSingleNode("//span[@class='nav__points']");
            var nLevel = doc.DocumentNode.SelectSingleNode("//span[@class='nav__button nav__button--is-dropdown'].span[@title='*']");

            if (nCoal == null || nLevel == null)
                return false;

            Points = int.Parse(nCoal.InnerText);
            Level = int.Parse(nLevel.InnerText.Substring("Level ".Length));
            return true;
        }
    }
}
