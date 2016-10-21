using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Web;

namespace SteamMultiAccount
{
    class WebClient
    {
        internal static int MaxRetries = 3;
        internal CookieContainer cookieContainer = new CookieContainer();
        private static TimeSpan timeout = TimeSpan.FromSeconds(60);
        private HttpClient client;

        internal WebClient()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            client = new HttpClient(handler) { Timeout = timeout };
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.8,en-GB;q=0.6");

            client.DefaultRequestHeaders.UserAgent.ParseAdd("SteamAccountHelper");
            ServicePointManager.DefaultConnectionLimit = 10;
            ServicePointManager.MaxServicePointIdleTime = 15000;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.ReusePort = true;
        }
        public async Task<HttpResponseMessage> GetContent(Uri url, Dictionary<string, string> data = null, HttpMethod method = null, CookieCollection cookies  = null,bool bAutoRedir = true)
        {
            if (method == null)
                method = HttpMethod.Get;

            HttpResponseMessage resp = null;
            HttpRequestMessage req = new HttpRequestMessage(method, url);
            if(data != null && data.Any())
            { 
                if(method != HttpMethod.Get)                        
                    try { 
                    req.Content = new FormUrlEncodedContent(data);  // Adding POST data
                    } catch(Exception e) {
                        Logging.LogToFile(e.Message);
                    }
                else
                    foreach (var _data in data)                     // If request is GET we add data to URL
                        req.RequestUri = req.RequestUri.AddQueryParameter(_data.Key, _data.Value);
            }

            if (cookies != null)
                    cookieContainer.Add(cookies);

            req.Headers.UserAgent.Add(client.DefaultRequestHeaders.UserAgent.First());

            do
            {
                if (resp != null)
                {
                    if (resp.Headers.Location == null)
                        break;
                    req = new HttpRequestMessage(HttpMethod.Get, resp.Headers.Location);
                }
                try {
                    resp = await client.SendAsync(req,HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
                } catch(Exception e) {
                    Logging.LogToFile("Cant getMessage "+e.Message );
                }
            } while (bAutoRedir); // While we have 'Location' in headers & bAutoRedir do redirect

            return resp;
        }
        public async Task<HtmlDocument> GetDocument(Uri url, Dictionary<string, string> data = null, HttpMethod method = null, CookieCollection cookies = null)
        {
            HtmlDocument ret = null;
            HttpResponseMessage resp = await GetContent(url, data, method, cookies).ConfigureAwait(false);
            if (!(resp.IsSuccessStatusCode && resp.Content != null))
                return null;

            string response = await resp.Content.ReadAsStringAsync();
            ret = new HtmlDocument();
            ret.LoadHtml(WebUtility.HtmlDecode(response));
            return ret;
        }
        public async Task<JObject> GetJObject(Uri url, Dictionary<string, string> data = null, HttpMethod method = null, CookieCollection cookies = null)
        {
            if (url == null)
                return null;

            JObject ret = null;
            HttpResponseMessage resp = await GetContent(url, data, method, cookies).ConfigureAwait(false);
            if (!(resp.IsSuccessStatusCode && resp.Content != null))
            {
                return null;
            }
            string response = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            ret = JObject.Parse(response);
            return ret;
        }
        public async Task<Uri> GetRedirectedUri(Uri uri)
        {
            if (uri == null)
                return null;

            HttpResponseMessage resp = null;
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Head, uri);
            try
            {
                resp = await client.SendAsync(req).ConfigureAwait(false);
            }
            catch (Exception e) { Logging.LogToFile("Cant get response: " + e.Message); }
            if (resp == null)
                return null;
            return resp.Headers.Location;
        }
        
        public async Task<HtmlDocument> GetDocument(HttpResponseMessage resp)
        {
            var response = await resp.Content.ReadAsStringAsync();
            var ret = new HtmlDocument();
            ret.LoadHtml(WebUtility.HtmlDecode(response));
            return ret;
        }

    }
    public static class UriExtensions
    {
        public static Uri AddQueryParameter(this Uri uri, string name, object value)
        {
            var builder = new UriBuilder(uri);
            if (builder.Query != null && builder.Query.Length > 1)
            {
                builder.Query = string.Format("{0}&{1}={2}", builder.Query.Substring(1), name, value);
            }
            else
            {
                builder.Query = string.Format("{0}={1}", name, value);
            }
            return builder.Uri;
        }
    }
}
