using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace SteamMultiAccount
{
    class WebClient
    {
        internal static int MaxRetries = 3;
        internal Dictionary<string, string> Cookie = new Dictionary<string, string>(4);
        private static TimeSpan timeout = TimeSpan.FromSeconds(60);
        private HttpClient client = new HttpClient(new HttpClientHandler() {UseCookies = false}) {Timeout = timeout};

        internal WebClient()
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SteamAccountHelper");
            ServicePointManager.DefaultConnectionLimit = 10;
            ServicePointManager.MaxServicePointIdleTime = 15000;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.ReusePort = true;
        }

        public async Task<HttpResponseMessage> GetContent(Uri url, Dictionary<string, string> data = null, HttpMethod method = null,string refferer = null)
        {
            if (method == null)
                method = HttpMethod.Get;

            HttpResponseMessage resp = null;
            using (HttpRequestMessage req = new HttpRequestMessage(method,url))
            {
                if(data!=null && data.Any())
                    try { 
                    req.Content = new FormUrlEncodedContent(data);
                    } catch(Exception e) {
                        Loging.LogToFile(e.Message);
                    }

                if (Cookie.Count>0){
                    StringBuilder sb = new StringBuilder();
                    foreach (KeyValuePair<string,string> cookie in Cookie)
                    {
                        sb.Append(cookie.Key + "=" + cookie.Value + ";");
                    }
                    req.Headers.Add("Cookie", sb.ToString());
                }

                if(!string.IsNullOrEmpty(refferer))
                req.Headers.Referrer = new Uri(refferer);

                try {
                    resp = await client.SendAsync(req).ConfigureAwait(false);
                } catch(Exception e) {
                    Loging.LogToFile("Cant getMessage "+e.Message );
                }
            }
            return resp;
        }
        public async Task<HtmlDocument> GetDocument(Uri url,HttpMethod method = null,Dictionary<string,string> data = null)
        {
            HtmlDocument ret = null;
            HttpResponseMessage resp = await GetContent(url, data, method).ConfigureAwait(false);
            if (!(resp.IsSuccessStatusCode && resp.Content != null))
            {
                return null;
            }
            string response = await resp.Content.ReadAsStringAsync();
            ret = new HtmlDocument();
            ret.LoadHtml(WebUtility.HtmlDecode(response));
            return ret;
        }
        public async Task<JObject> GetJObject(Uri url, HttpMethod method = null, Dictionary<string, string> data = null)
        {
            if (url == null)
                return null;

            JObject ret = null;
            HttpResponseMessage resp = await GetContent(url, data, method).ConfigureAwait(false);
            if (!(resp.IsSuccessStatusCode && resp.Content != null))
            {
                return null;
            }
            string response = await resp.Content.ReadAsStringAsync();
            ret = JObject.Parse(response);
            return ret;
        }
    }
}
