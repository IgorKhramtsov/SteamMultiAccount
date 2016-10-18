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

namespace SteamMultiAccount
{
    internal class GiveawayBot
    {
        internal WebClient WebClient;
        internal BotConfig Config;
        internal CookieCollection Cookie;
        internal GiveawayBot(WebClient webClient,BotConfig config)
        {
            if (webClient == null || config == null)
            {
                Logging.LogToFile("WebClient is null");
                return;
            }

            this.Cookie = new CookieCollection();
            this.WebClient = webClient;
            this.Config = config;
        }

        private GiveawayBot() { }
    }
    internal sealed class GameminerBot : GiveawayBot
    {
        const string GameMinerURL = "http://gameminer.net/";
        const string GameMinerHost = "gameminer.net";
        const string GameMinerAPI = GameMinerURL+"api/giveaways/";
        const string GameMinerWon = GameMinerURL+"giveaways/won/";
        const string GameMinerCoal = GameMinerAPI+"coal";
        const string GameMinerGold = GameMinerAPI + "gold";
        const string GameMinerEnter = GameMinerURL + "giveaway/enter/";

        private int iCoal;
        private int iLevel;
        private string sUsername;
        private string xsrf;
        internal GameminerBot(WebClient client,BotConfig config) : base(client,config)
        {
            
        }

        internal async Task<bool> Init()
        {
            if (!await Login().ConfigureAwait(false))
                return false;
            return true;
        }
        internal async Task<bool> RefreshProfile()
        {
            var resp = await WebClient.GetContent(new Uri(GameMinerURL)).ConfigureAwait(false);
            if (resp == null)
                return false;
            if (!string.IsNullOrWhiteSpace(resp.Headers.GetValues("set-cookie").ToArray()[0].ToString()))
            {
                var cookies = resp.Headers.GetValues("set-cookie").ToList();
                foreach(var cookie in cookies)
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
        internal async Task CheckGiveaways()
        {
            int LastPage = 0;
            int Page = 1;
            while (Page <= LastPage || LastPage == 0)
            {
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
                    response = await WebClient.GetJObject(new Uri(GameMinerCoal), Data,cookies: Cookie).ConfigureAwait(false);
                if (response == null)
                    continue;
                List<Task> tasks = new List<Task>();
                foreach(var giveaway in response["giveaways"])
                {
                    var code = giveaway["code"].Value<string>();
                    if (string.IsNullOrWhiteSpace(code))
                        continue;
                    await EnterGiveaway(code).ConfigureAwait(false);
                    //tasks.Add(EnterGiveaway(code));
                }
                //await Task.WhenAll(tasks).ConfigureAwait(false);
                if (LastPage == 0)
                    LastPage = response["last_page"].Value<int>();
            }
        }

        internal async Task<bool> Login()
        {
            string loginURL = GameMinerURL + "login/steam?backurl=http%3A%2F%2Fgameminer.net%2F&agree=True";
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
            var Hresp = await WebClient.GetContent(new Uri("https://steamcommunity.com/openid/login"), Data, HttpMethod.Post,null,false);
            if (Hresp == null)
                return false;
            var url = Hresp.Headers.Location;
            Hresp = null;
            Hresp = await WebClient.GetContent(url,bAutoRedir: false);
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
        internal async Task EnterGiveaway(string code)
        {
            await Task.Delay(new Random().Next(1, 5) * 1000).ConfigureAwait(false); // Random delay 1-5 sec
            var data = new Dictionary<string, string>()
            {
                { "_xsrf", xsrf },
                { "json", "true" }
            };

            var response = await WebClient.GetJObject(new Uri(GameMinerEnter + code), data,HttpMethod.Post,Cookie);
            if (response == null)
                return;
            if (response["status"].Value<string>() != "ok")
            {
                Logging.LogToFile("Cant enter to giveawat: " + response.ToString());
                return;
            }
            iCoal = response["coal"].Value<int>();
            Logging.LogToFile("Giveaway successfully entered, coal = " + iCoal);
        }
    }
}
