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

namespace SteamMultiAccount
{
    internal class WebBot
    {
        internal sealed class Item_desc
        {
            [JsonProperty(Required = Required.DisallowNull)]
            internal string classid { get; set; } //Id in class
            [JsonProperty(Required = Required.DisallowNull)]
            internal string instanceid { get; set; } //Class
            [JsonProperty(Required = Required.DisallowNull)]
            internal string market_hash_name { get; set; } //Name for price sheet
            [JsonProperty(Required = Required.DisallowNull)]
            internal bool marketable { get; set; } //Are markettable?
            [JsonProperty(Required = Required.DisallowNull)]
            internal string Name { get; set; }//Readable name
        }
        internal sealed class SteamItem
        {
            //https://developer.valvesoftware.com/wiki/Steam_Web_API/IEconService#CEcon_Asset

            [JsonProperty(Required = Required.DisallowNull)]
            internal string assetid { get; set; }//Either assetid or currencyid will be set
            [JsonProperty(Required = Required.DisallowNull)]
            internal string id
            {
                get { return assetid; }
                set { assetid = value; }
            }
            [JsonProperty(Required = Required.DisallowNull)]
            internal string classid { get; set; } //Together with instanceid, uniquely identifies the display of the item
            [JsonProperty(Required = Required.DisallowNull)]
            internal string instanceid { get; set; }//Together with classid, uniquely identifies the display of the item
        }
        internal struct _Item
        {
            internal string market_name;
            internal string asset_id;
            internal string name;
        }

        private SteamID steamID;
        private WebClient webClient;
        private bool Initialized = false;
        private Bot _bot;
        internal List<ulong> appidToFarmSolo;
        internal List<ulong> appidToFarmMulti;
        internal const string SteamCommunityURL = "https://steamcommunity.com";

        internal string sessionID;
        internal WebBot()
        {
            appidToFarmSolo = new List<ulong>();
            appidToFarmMulti = new List<ulong>();
        }

        internal async Task Init(Bot bot, string webAPIUserNonce)
        {
            _bot = bot;
            steamID = _bot.steamClient.SteamID;
            webClient = new WebClient();

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
                            Encoding.ASCII.GetString(System.Net.WebUtility.UrlEncodeToBytes(cryptedSessionKey, 0,
                                cryptedSessionKey.Length)),
                        encrypted_loginkey:
                            Encoding.ASCII.GetString(System.Net.WebUtility.UrlEncodeToBytes(cryptedLoginKey, 0,
                                cryptedLoginKey.Length)),
                        method: System.Net.WebRequestMethods.Http.Post,
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

            string steamLogin = autResult["token"].AsString();
            string steamLoginSecure = autResult["tokensecure"].AsString();

            webClient.Cookie["sessionid"] = sessionID;
            webClient.Cookie["steamLogin"] = steamLogin;
            webClient.Cookie["steamLoginSecure"] = steamLoginSecure;

            webClient.Cookie["webTradeEligibility"] = "{\"allowed\":0,\"reason\":0,\"allowed_at_time\":0,\"steamguard_required_days\":0,\"sales_this_year\":0,\"max_sales_per_year\":0,\"forms_requested\":0}";

            Initialized = true;
        }
        internal bool isInitialized()
        {
            return Initialized;
        }
        internal async Task RefreshGamesToFarm()
        {
            byte Pagecount = 1;//Count of badges page by default

            if (!isInitialized())//Is webBot initialize?
                return;

            Uri url = new Uri(SteamCommunityURL + "/profiles/" + steamID.ConvertToUInt64() + "/badges");//Uri to page with badges
            HtmlDocument doc = null;

            for (int i = 0; doc == null && i < WebClient.MaxRetries; i++)
                doc = await webClient.GetDocument(url).ConfigureAwait(false);//Get first page with badges
            if (doc == null)
            {
                _bot.Log("Cant get badge page", LogType.Error);
                return;
            }

            HtmlNodeCollection collection = doc.DocumentNode.SelectNodes("//a[@class='pagelink']");//Are we have page navigation?
            if (collection != null)
                if (!Byte.TryParse(collection.Last().InnerText, out Pagecount))//If yes, change ours count of page
                    Pagecount = 1;

            appidToFarmMulti.Clear();//Clear up our list, because we will check badges level every N time
            appidToFarmSolo.Clear();
            List<Task> tasks = new List<Task>(Pagecount);//Make list of task to check page for game to idle
            for (byte i = 1; i <= Pagecount; i++)
            {
                byte currentPage = i;//Need save our page for use async
                HtmlDocument page = await webClient.GetDocument(new Uri(url, "?p=" + currentPage)).ConfigureAwait(false);
                tasks.Add(CheckPage(page));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);//Wait for all page checked

            if (!appidToFarmSolo.Any() && !appidToFarmMulti.Any())
                _bot.Log("Have nothing to farm", LogType.Info);
            else
                _bot.Log($"We have {appidToFarmSolo.Count} to farm solo and {appidToFarmMulti.Count} to farm multi", LogType.Info);//Log count of game to idle
        }
        internal async Task CheckPage(HtmlDocument page)
        {
            if (page == null)
                return;

            HtmlNodeCollection BadgeStats = page.DocumentNode.SelectNodes("//div[@class='badge_title_stats']");//Get badge block
            foreach (HtmlNode node in BadgeStats)
            {
                HtmlNode PlayButton = node.SelectSingleNode(".//a[@class='btn_green_white_innerfade btn_small_thin']");//Get play button with appid for game needed idling
                HtmlNode PlayHours = node.SelectSingleNode(".//div[@class='badge_title_stats_playtime']");//Get stats of playing hours to select how we will farm multi or single
                if (PlayButton == null)
                    continue;//Doesnt need to farm, no cards remaining
                string appid_row = PlayButton.GetAttributeValue("href", "");
                if (appid_row == "")
                    continue;
                appid_row = appid_row.Substring("steam://run/".Length);//Get appid
                ulong appid;
                if (!ulong.TryParse(appid_row, out appid))//Parse appid
                    _bot.Log("Cant parse appid from - " + appid_row, LogType.Error);

                float hours;

                Match match = Regex.Match(PlayHours.InnerText, @"[0-9\.,]+");//Parse time in game
                if (match.Success)
                    if (float.TryParse(match.Value, System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out hours))
                        if (hours > 3) //if more than 3 hours then we add it to solo farming list
                            if (!appidToFarmSolo.Contains(appid))
                            {
                                appidToFarmSolo.Add(appid);
                                continue;
                            }
                if (!appidToFarmMulti.Contains(appid))//Check if we already have this game in list
                    appidToFarmMulti.Add(appid);
            }
        }
        internal async Task<int> getPrice(_Item item)
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

            JToken jToken = jObject["prices"];//Get token with price value
            if (jToken == null)
            {
                _bot.Log("JToken is null", LogType.Error);
                return 0;
            }
            float price = 0;
            int PriceCount = 0;
            for (JToken token = jToken.Last; token != null && PriceCount < 50; token = token.Previous)//Get 50 prices from end
            {
                price += token[1].Value<float>();
                PriceCount++;
            }
            price /= PriceCount;
            return (int)(price * 100);
        }
        internal async Task<List<_Item>> GetTraddableItems()
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


            var items = new List<_Item>();
            foreach (SteamItem item in result_inv)
            {
                Item_desc a = result_desc.Find(x => (x.classid == item.classid) && (x.instanceid == item.instanceid) && (x.marketable == true));
                if (a != null)
                    items.Add(new _Item { market_name = a.market_hash_name, asset_id = item.assetid, name = a.Name });
            }
            return items;
        }
        internal async Task<string> SellItem(_Item item,int price = 0)
        {
            string sessionID;
            if (!webClient.Cookie.TryGetValue("sessionid", out sessionID))
                return null;

            string referrer = SteamCommunityURL + "/market";
            string request = referrer + "/sellitem";

            if (price == 0)//If price not set
            {
                price = await getPrice(item).ConfigureAwait(false);//Get average price
                if (price == 0)
                    return string.Empty;
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
                response = await webClient.GetContent(new Uri(request), data, HttpMethod.Post,referrer).ConfigureAwait(false);
            if (response == null)
            {
                _bot.Log($"Request failed even after {WebClient.MaxRetries} tries", LogType.Error);
                return string.Empty;
            }
            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return $"Предммет \"{item.name}\" будет выставлен на продажу за {(float)(price) / 100} Руб. пожалуйста потвердите лот.";
                case System.Net.HttpStatusCode.BadGateway:
                    return $"Предмет \"{item.name}\" ожидает потверждения.";
                default:
                    return $"Ошибка при выставлении \"{item.name}\" на продажу ({response.StatusCode})";
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
    }
}
