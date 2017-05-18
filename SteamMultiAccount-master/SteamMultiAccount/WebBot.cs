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
    internal class WebBot
    {
        internal sealed class Item_desc
        {
            [JsonProperty(Required = Required.DisallowNull)]
            internal string classid { get; set; } // Id in class
            [JsonProperty(Required = Required.DisallowNull)]
            internal string instanceid { get; set; } // Class
            [JsonProperty(Required = Required.DisallowNull)]
            internal string market_hash_name { get; set; } // Name for price sheet
            [JsonProperty(Required = Required.DisallowNull)]
            internal bool marketable { get; set; } // Is markettable?
            [JsonProperty(Required = Required.DisallowNull)]
            internal string Name { get; set; } // Readable name
        }
        internal sealed class SteamItem
        {
            //https://developer.valvesoftware.com/wiki/Steam_Web_API/IEconService#CEcon_Asset

            [JsonProperty(Required = Required.DisallowNull)]
            internal string assetid { get; set; } // Either assetid or currencyid will be set
            [JsonProperty(Required = Required.DisallowNull)]
            internal string id
            {
                get { return assetid; }
                set { assetid = value; }
            }
            [JsonProperty(Required = Required.DisallowNull)]
            internal string classid { get; set; } // Together with instanceid, uniquely identifies the display of the item
            [JsonProperty(Required = Required.DisallowNull)]
            internal string instanceid { get; set; }// Together with classid, uniquely identifies the display of the item
        }
        internal class Item
        {
            internal string market_name;
            internal string asset_id;
            internal string name;
            internal int price;
        }

        private SteamID steamID;
        internal WebClient webClient;
        internal GameminerBot gameminerBot;
        public bool Initialized { get; private set; } = false;
        private Bot _bot;
        internal List<Game> GamesToFarmSolo;
        internal List<Game> GamesToFarmMulti;
        internal List<ulong> alreadyHaveSubID;
        internal bool bWatchBroadcast;
        internal const string SteamCommunityURL = "https://steamcommunity.com";
        internal const string SteamCommunityHOST = "steamcommunity.com";

        internal string sessionID;
        internal WebBot(Bot bot)
        {
            GamesToFarmSolo = new List<Game>();
            GamesToFarmMulti = new List<Game>();
            alreadyHaveSubID = new List<ulong>();
            webClient = new WebClient();
            this._bot = bot;
        }

        internal async Task Init(string webAPIUserNonce)
        {
            steamID = _bot.steamClient.SteamID;

            sessionID = Convert.ToBase64String(Encoding.UTF8.GetBytes(steamID.ToString()));

            // Generate an AES session key
            byte[] sessionKey = CryptoHelper.GenerateRandomBlock(32);

            // RSA encrypt it with the public key for the universe we're on
            byte[] cryptedSessionKey;
            using (var crypto = new RSACrypto(KeyDictionary.GetPublicKey(_bot.steamClient.ConnectedUniverse)))
            {
                cryptedSessionKey = crypto.Encrypt(sessionKey);
            }
            // Copy our login key
            byte[] loginKey = new byte[webAPIUserNonce.Length];
            Array.Copy(Encoding.ASCII.GetBytes(webAPIUserNonce), loginKey, webAPIUserNonce.Length);

            // AES encrypt the loginkey with our session key
            byte[] cryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);

            _bot.Log("Logging in to ISteamUserAuth", LogType.Info);

            KeyValue autResult;

            using (dynamic iSteamUserAuth = WebAPI.GetInterface("ISteamUserAuth"))
            {
                iSteamUserAuth.Timeout = 60000;
                try
                {
                    autResult = iSteamUserAuth.AuthenticateUser(
                        steamid: steamID.ConvertToUInt64(),
                        sessionkey:
                            Encoding.ASCII.GetString(WebUtility.UrlEncodeToBytes(cryptedSessionKey, 0,
                                cryptedSessionKey.Length)),
                        encrypted_loginkey:
                            Encoding.ASCII.GetString(WebUtility.UrlEncodeToBytes(cryptedLoginKey, 0,
                                cryptedLoginKey.Length)),
                        method: WebRequestMethods.Http.Post,
                        secure: true);
                }
                catch (Exception e)
                {
                    _bot.Log("Cant AuthenticateUser " + e.Message, LogType.Error);
                    return;
                }
            }
            if (autResult == null)
                return;
            _bot.Log("Success", LogType.Info);

            string steamLogin = autResult["token"].Value;
            string steamLoginSecure = autResult["tokensecure"].Value;

            webClient.cookieContainer.Add(new Cookie("sessionid", sessionID, "/", "." + SteamCommunityHOST));
            webClient.cookieContainer.Add(new Cookie("steamLogin", steamLogin, "/", "." + SteamCommunityHOST));
            webClient.cookieContainer.Add(new Cookie("steamLoginSecure", steamLoginSecure, "/", "." + SteamCommunityHOST));

            gameminerBot = new GameminerBot(webClient, _bot.BotConfig);

            Initialized = true;
            //GiveawayBotInit().Forget();
        }
        internal async Task<bool> RefreshGamesToFarm()
        {
            if (!Initialized) // Is webBot initialize?
                return false;

            byte Pagecount = 1; // Count of badges page by default
            await _bot.RefreshSessionIfNeeded().ConfigureAwait(false);

            Uri url = new Uri(SteamCommunityURL + "/profiles/" + steamID.ConvertToUInt64() + "/badges"); // Uri to page with badges

            HtmlDocument doc = null;
            for (int i = 0; doc == null && i < WebClient.MaxRetries; i++)
                doc = await webClient.GetDocument(url).ConfigureAwait(false); // Get first page with badges
            if (doc == null)
            {
                _bot.Log("Cant get badge page", LogType.Error);
                return false;
            }

            HtmlNodeCollection collection = doc.DocumentNode.SelectNodes("//a[@class='pagelink']"); // Are we have page navigation?
            if (collection != null)
                if (!byte.TryParse(collection.Last().InnerText, out Pagecount)) // If yes, change ours count of page
                    Pagecount = 1;

            GamesToFarmMulti.Clear(); // Clear up our list, because we will check badges level every N time
            GamesToFarmSolo.Clear();
            List<Task> tasks = new List<Task>(Pagecount); // Make list of task to check page for game to idle
            for (byte i = 1; i <= Pagecount; i++)
            {
                byte currentPage = i; // Need save our page for use async
                HtmlDocument page = await webClient.GetDocument(new Uri(url, "?p=" + currentPage)).ConfigureAwait(false);
                tasks.Add(CheckPage(page));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false); // Wait for all page checked

            if (!GamesToFarmSolo.Any() && !GamesToFarmMulti.Any())
                _bot.Log("Have nothing to farm", LogType.Info);
            else
                _bot.Log($"We have {GamesToFarmSolo.Count} to farm solo and {GamesToFarmMulti.Count} to farm together", LogType.Info); // Log count of game to idle

            return true;
        }
        internal async Task<bool> CheckPage(HtmlDocument page)
        {
            if (page == null)
                return false;

            HtmlNodeCollection BadgeStats = page.DocumentNode.SelectNodes("//div[@class='badge_title_row']"); // Get badge block
            foreach (HtmlNode node in BadgeStats)
            {
                HtmlNode PlayButton = node.SelectSingleNode(".//div[@class='badge_title_stats']//a[@class='btn_green_white_innerfade btn_small_thin']"); // Get play button with appid for game needed idling
                HtmlNode PlayHours = node.SelectSingleNode(".//div[@class='badge_title_stats']//div[@class='badge_title_stats_playtime']"); // Get stats of playing hours to select how we will farm multi or single
                HtmlNode PlayName = node.SelectSingleNode(".//div[@class='badge_title']"); // Get name
                if (PlayButton == null)
                    continue; // Doesnt need to farm, no cards remaining
                string appid_row = PlayButton.GetAttributeValue("href", "");
                if (appid_row == "")
                    continue;
                appid_row = appid_row.Substring("steam://run/".Length); // Get appid
                ulong appid;
                if (!ulong.TryParse(appid_row, out appid)) // Parse appid
                    _bot.Log("Cant parse appid from - " + appid_row, LogType.Error);

                float hours;
                string name = "";
                if (PlayName != null && PlayName.FirstChild != null && !string.IsNullOrWhiteSpace(PlayName.FirstChild.InnerText))
                    name = PlayName.FirstChild.InnerText;
                Game game = new Game(appid, name.Replace("\t", "").Replace("\r", "").Replace("\n", ""));


                Match match = Regex.Match(PlayHours.InnerText, @"[0-9\.,]+"); // Parse time in game
                if (match.Success)
                {
                    if (float.TryParse(match.Value, System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out hours))
                        if (hours > 3) // If more than 3 hours then we add it to solo farming list
                        {
                            if (!GamesToFarmSolo.Contains(game))
                                GamesToFarmSolo.Add(game);
                        }
                        else if (!GamesToFarmMulti.Contains(game))
                            GamesToFarmMulti.Add(game);
                }
            }
            return true;
        }
        internal async Task<int> getPrice(Item item)
        {
            JObject jObject = null;
            item.market_name = item.market_name.Replace("&", "%26");
            for (byte i = 0; i < WebClient.MaxRetries && jObject == null; i++)//Get price history
                jObject = await webClient.GetJObject(new Uri(SteamCommunityURL + "/market/pricehistory/?appid=753&market_hash_name=" + item.market_name)).ConfigureAwait(false);

            if (jObject == null)
            {
                _bot.Log($"Cant get price history even after {WebClient.MaxRetries} tries", LogType.Error);
                return 0;
            }

            JToken jToken = jObject["prices"]; // Get token with price value
            if (jToken == null)
            {
                _bot.Log("JToken is null", LogType.Error);
                return 0;
            }
            float price = 0;
            int PriceCount = 0;
            for (JToken token = jToken.Last; token != null && PriceCount < 50; token = token.Previous) // Get 50 prices from end
            {
                price += token[1].Value<float>();
                PriceCount++;
            }
            price /= PriceCount;
            item.price = (int)(price * 100);
            return (int)(price * 100);
        }
        internal async Task<List<Item>> GetTraddableItems()
        {
            JObject jObject = null;
            for (byte i = 0; i < WebClient.MaxRetries && jObject == null; i++)
                jObject = await webClient.GetJObject(new Uri(SteamCommunityURL + "/my/inventory/json/753/6?marketable=1")).ConfigureAwait(false);

            if (jObject == null)
            {
                _bot.Log($"Cant get inventory event after {WebClient.MaxRetries} tries", LogType.Error);
                return null;
            }

            IEnumerable<JToken> jTokens_desc = jObject.SelectTokens("$.rgDescriptions.*");
            if (jTokens_desc == null)
            {
                _bot.Log("JToken is null", LogType.Error);
                return null;
            }

            bool iscard;
            var result_desc = new List<Item_desc>();
            foreach (JToken jToken in jTokens_desc)
            {
                iscard = false;
                foreach (JToken token in jToken["tags"])
                    if (token["name"].Value<string>() == "Trading Card")
                    {
                        iscard = true;
                        break;
                    }

                if (iscard == false)
                    continue;

                try
                {
                    result_desc.Add(JsonConvert.DeserializeObject<Item_desc>(jToken.ToString()));
                }
                catch (Exception e)
                {
                    _bot.Log("Cant get item descriptions " + e.Message, LogType.Error);
                }
            }

            IEnumerable<JToken> jTokens_inv = jObject.SelectTokens("$.rgInventory.*");
            if (jTokens_inv == null)
            {
                _bot.Log("JToken is null", LogType.Error);
                return null;
            }

            var result_inv = new List<SteamItem>();
            foreach (JToken jToken in jTokens_inv)
            {
                try
                {
                    result_inv.Add(JsonConvert.DeserializeObject<SteamItem>(jToken.ToString()));
                }
                catch (Exception e)
                {
                    _bot.Log("Cant desiarilize 'SteamItem' " + e.Message, LogType.Error);
                }
            }


            var items = new List<Item>();
            foreach (SteamItem item in result_inv)
            {
                Item_desc a = result_desc.Find(x => (x.classid == item.classid) && (x.instanceid == item.instanceid) && (x.marketable == true));
                if (a != null)
                    items.Add(new Item { market_name = a.market_hash_name, asset_id = item.assetid, name = a.Name });
            }
            return items;
        }
        internal async Task<bool> SellItem(Item item,int price = 0)
        {
            // TODO: Accepting trades from mobile auth

            await _bot.RefreshSessionIfNeeded().ConfigureAwait(false);
            string sessionID = "";
            var cookies = webClient.cookieContainer.GetCookies(new Uri(SteamCommunityURL));
            try
            {
                sessionID = cookies["sessionid"].Value;
            }
            catch (Exception e) { Logging.LogToFile("Cookie doesnt exist: " + e.Message); return false; }
            if (string.IsNullOrEmpty(sessionID))
            {
                _bot.Log("sessionID isnt set",LogType.Error);
                return false;
            }

            string request = SteamCommunityURL + "/market/sellitem";
            Uri referrer = new Uri(SteamCommunityURL + "/profiles/" + steamID.ConvertToUInt64() + "/inventory/");

            // Order of price:
            // 1) Function argument
            // 2)  Item field
            // 3)   Function result
            if (price == 0)
                price = (item.price != 0) ? item.price : await getPrice(item).ConfigureAwait(false);
            if (price == 0)
            {
                _bot.Log($"Cant get price of \"{item.name}\"", LogType.Error);
                return false;
            }

            Dictionary<string, string> data = new Dictionary<string, string>(6) {
                    {"sessionid", sessionID},
                    {"appid", "753"},
                    {"contextid", "6"},
                    {"assetid", item.asset_id},
                    {"amount", "1"},
                    {"price", price.ToString()}
                };

            HttpResponseMessage response = null;
            for (byte i = 0; i < WebClient.MaxRetries && response == null; i++)
                response = await webClient.GetContent(new Uri(request), data, HttpMethod.Post,referrer: referrer).ConfigureAwait(false);
            if (response == null)
            {
                _bot.Log($"Request for selling failed even after {WebClient.MaxRetries} tries", LogType.Error);
                return false;
            }
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return true;
                case HttpStatusCode.BadGateway:
                    return true;
                default:
                    _bot.Log($"\"{item.name}\" error while selling: ({response.StatusCode})", LogType.Error);
                    return false;
            }
        }
        internal async Task<string> AddToCart(string subID)
        {
            ulong uSubID;
            if (!ulong.TryParse(subID, out uSubID))
                return "Invalid subID("+subID+")";

            if (!alreadyHaveSubID.Any())
            {
                var response = await webClient.GetJObject(new Uri("http://store.steampowered.com/dynamicstore/userdata/"));
                if (response == null)
                    return "Cant get user data.";
                alreadyHaveSubID.AddRange(response["rgOwnedPackages"].Values<ulong>());
            }
            if (alreadyHaveSubID.Contains(uSubID))
                return "already have";
            string Url = "http://store.steampowered.com/cart/";
            var Data = new Dictionary<string, string>(3)
            {
                {"action","add_to_cart"},
                {"sessionid",sessionID},
                {"subid",subID}
            };

            var resp = await webClient.GetContent(new Uri(Url), Data, HttpMethod.Post).ConfigureAwait(false);
            if (resp == null)
                return "Cant add subid(" + subID + ")";
            var cookies = resp.Headers.GetValues("set-cookie").ToList();
            foreach (string cookie in cookies)
            {
                
                if (cookie.Contains("shoppingCartGID="))
                {
                    // TODO: Check if cookie container auto apply cookies
                    // so we dont need this shit if yes
                    var cookieValue = cookie.Substring(cookie.IndexOf("shoppingCartGID=") + "shoppingCartGID=".Length,
                             cookie.IndexOf("; expires") - "shoppingCartGID=".Length);
                    webClient.cookieContainer.Add(new Cookie("shoppingCartGID", cookieValue, "/", "." + SteamCommunityHOST));
                    break;
                }
                
            }
            return "SubID("+subID+") was added to cart.";
        }
        internal async Task<string> BuyCart()
        {
            var cookies = webClient.cookieContainer.GetCookies(new Uri(SteamCommunityURL));
            string shopingCart = "";
            try
            {
                shopingCart = cookies["shoppingCartGID"].Value;
            } catch (Exception e) { Logging.LogToFile("Cookie doesnt exist: " + e.Message);return ""; }
            if (string.IsNullOrEmpty(shopingCart))
                return "";
            string initTransactionURL = "https://store.steampowered.com/checkout/inittransaction/";
            var Data = new Dictionary<string,string>()
            {
                {"gidShoppingCart",shopingCart },
                {"gidReplayOfTransID","-1" },
                {"PaymentMethod","steamaccount" },
                {"Country","RU" },
                {"ShippingCountry","RU" },
                {"bUseRemainingSteamAccount","1" }
            };
            
            var resp = await webClient.GetJObject(new Uri(initTransactionURL), Data, HttpMethod.Post).ConfigureAwait(false);
            if (resp == null)
                return "Cant init transaction.";

            string finilizeTransURL = "https://store.steampowered.com/checkout/finalizetransaction/";
            Data = new Dictionary<string, string>()
            {
                {"transid", resp["transid"].Value<string>() }
            };

            var resp2 = await webClient.GetJObject(new Uri(finilizeTransURL), Data, HttpMethod.Post).ConfigureAwait(false);
            if (resp2 == null)
                return "Cant finilize transaction.";

            if (resp2["success"].Value<string>() == "22")
                return "Cart was bought.";
            if (resp2["success"].Value<string>() == "2")
                return "Cart wasnt bought!";
            return "Cart bought status: " + resp2["success"].Value<string>()+".";
        }
        internal async Task<bool> WatchBroadcast(string steamID)
        {
            // TODO: fix - Bot listed in chat but not in broadcast
            string BroadCastURL = "http://steamcommunity.com/broadcast/getbroadcastmpd/" + steamID;
            Dictionary<string, string> Data = new Dictionary<string, string>(3)
            {
                { "steamid" , steamID},
                { "broadcastid" , "0" },
                { "viewertoken" , "0" }
            };
            JObject Response = null;
            Response = await webClient.GetJObject(new Uri(BroadCastURL),Data).ConfigureAwait(false);
            if (Response == null)
                return false;

            string BroadcastID = Response["broadcastid"].Value<string>();
            string ViewerToken = Response["viewertoken"].Value<string>();
            
            Data = new Dictionary<string, string>(2)
            {
                {"steamid", steamID},
                {"broadcastid", BroadcastID}
            };
            
            Response = null;
            Response = await webClient.GetJObject(new Uri("http://steamcommunity.com/broadcast/getchatinfo/"),Data).ConfigureAwait(false);
            if (Response == null)
                return false;
            string url = Response["view_url"].Value<string>().Replace("messages/", "messages/0");
            bWatchBroadcast = true;
            Task.Run(() => WatchBroadcastThread(url,steamID,BroadcastID,ViewerToken)).Forget();
            return true;
        }
        private async Task WatchBroadcastThread(string url,string steamid,string broadcastid,string viewertoken)
        {
            var Response = await webClient.GetJObject(new Uri(url)).ConfigureAwait(false);
            if (Response == null)
                return;
            string nextRequest = Response["next_request"].Value<string>();
            int delay = Response["initial_delay"].Value<int>();
            while (Response != null && bWatchBroadcast)
            {
                Thread.Sleep(delay);
                Response = null;
                string currNumberReq = url.Substring(url.IndexOf("messages/") + "messages/".Length, url.IndexOf("?viewer=") - (url.IndexOf("messages/") + "messages/".Length)); // current request number
                url = url.Replace("messages/"+ currNumberReq, "messages/"+nextRequest);
                for (byte i = 0; i < 3 || Response != null; i++)
                {
                    Response = await webClient.GetJObject(new Uri(url)).ConfigureAwait(false);
                }
            }
            string a = url.Substring(url.IndexOf("messages/") + "messages/".Length, url.IndexOf("?viewer=") - (url.IndexOf("messages/") + "messages/".Length)); // current request number
            url = url.Replace("messages/" + a, "messages/0");
            if (bWatchBroadcast)
            {
                string BroadCastURL = "http://steamcommunity.com/broadcast/getbroadcastmpd/" + steamID;
                Dictionary<string, string> Data = new Dictionary<string, string>(3)
                {
                    {"steamid", steamid},
                    {"broadcastid", broadcastid},
                    {"viewertoken", viewertoken}
                };

                Response = null;
                Response = await webClient.GetJObject(new Uri(BroadCastURL), Data).ConfigureAwait(false);
                if (Response == null)
                    return;
                broadcastid = Response["broadcastid"].Value<string>();
                viewertoken = Response["viewertoken"].Value<string>();
                Task.Run(() => WatchBroadcastThread(url,steamid,broadcastid,viewertoken)).Forget();
            }

        }
        internal async Task<bool> AcceptGift(ulong appID)
        {
            if (appID == 0 || !Initialized)
                return false;

            string request = SteamCommunityURL + "/gifts/" + appID + "/acceptunpack";
            Dictionary<string, string> data = new Dictionary<string, string>(1) {
                { "sessionid", sessionID }
            };

            HttpResponseMessage resp = null;
            for (byte i = 0; i < WebClient.MaxRetries && resp == null; i++)
                resp = await webClient.GetContent(new Uri(request), data, HttpMethod.Post).ConfigureAwait(false);

            if (resp == null)
            {
                _bot.Log("Cant get response even after " + WebClient.MaxRetries, LogType.Error);
                return false;
            }
            return true;
        }
        internal async Task<bool> IsLoggedIn()
        {
            if (!Initialized)
                return false;
            var redirectedURI = await webClient.GetRedirectedUri(new Uri(SteamCommunityURL + "/my/videos")).ConfigureAwait(false);
            if (redirectedURI.ToString().Contains("/login"))
                return false;

            return true;
        }
        internal async Task GiveawayBotInit()
        {
            _bot.Log("Gameminer bot starting...",LogType.Info);
            if (!await gameminerBot.Init().ConfigureAwait(false))
            {
                _bot.Log("Fail", LogType.Info);
                return;
            }
            _bot.Log("Success", LogType.Info);
            _bot.Log("Checking giveaways...", LogType.Info);
            gameminerBot.CheckGiveaways().Forget();
        }
    }
}
