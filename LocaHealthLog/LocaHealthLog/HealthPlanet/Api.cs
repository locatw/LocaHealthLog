using AngleSharp.Parser.Html;
using AngleSharp.Dom.Html;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace LocaHealthLog.HealthPlanet
{
    class Api
    {
        private static readonly string host = "https://www.healthplanet.jp";

        private static readonly string successUrl = $"{host}/success.html";

        private static readonly string loginUrl = $"{host}/login_oauth.do";

        private static readonly string authUrl = $"{host}/oauth/auth";

        private static readonly string approveUrl = $"{host}/oauth/approval.do";

        private static readonly string tokenUrl = $"{host}/oauth/token";

        private static readonly string innerScanStatusUrl = $"{host}/status/innerscan.json";

        private static readonly string redirectUri = "https://localhost";

        private static HttpClient client = new HttpClient(new HttpClientHandler() { UseCookies = true });

        private string userAgent;

        public Api(string userAgent)
        {
            this.userAgent = userAgent;
        }

        public async Task LoginAsync(string loginId, string password)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(loginUrl);
                request.Headers.UserAgent.ParseAdd(userAgent);
                request.Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>("loginId", loginId),
                    new KeyValuePair<string, string>("passwd", password),
                    new KeyValuePair<string, string>("send", "1"),
                    new KeyValuePair<string, string>("url", successUrl),
                });

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public async Task<string> AuthenticateAsync(string clientId)
        {
            var queryParams = HttpUtility.ParseQueryString(String.Empty);
            queryParams["client_id"] = clientId;
            queryParams["redirect_uri"] = redirectUri;
            queryParams["scope"] = "innerscan";
            queryParams["response_type"] = "code";

            var uriBuilder = new UriBuilder(authUrl);
            uriBuilder.Query = queryParams.ToString();

            using (var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString()))
            {
                request.Headers.UserAgent.ParseAdd(userAgent);

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();

                    var contentStream = await response.Content.ReadAsStreamAsync();
                    using(var reader = new StreamReader(contentStream, Encoding.GetEncoding("Shift_JIS")))
                    {
                        string html = await reader.ReadToEndAsync();

                        return await ScrapeOAuthTokenAsync(html);
                    }
                }
            }
        }

        public async Task<string> ApproveAsync(string oAuthToken)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(approveUrl);
                request.Headers.UserAgent.ParseAdd(userAgent);
                request.Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>("approval", "true"),
                    new KeyValuePair<string, string>("oauth_token", oAuthToken),
                });

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();

                    var contentStream = await response.Content.ReadAsStreamAsync();
                    using(var reader = new StreamReader(contentStream, Encoding.GetEncoding("Shift_JIS")))
                    {
                        string html = await reader.ReadToEndAsync();

                        return await ScrapeCodeAsync(html);
                    }
                }
            }
        }

        public async Task<Token> GetTokenAsync(string clientId, string clientSecret, string code)
        {
            var queryParams = HttpUtility.ParseQueryString(String.Empty);
            queryParams["client_id"] = clientId;
            queryParams["client_secret"] = clientSecret;
            queryParams["redirect_uri"] = successUrl;
            queryParams["code"] = code;
            queryParams["grant_type"] = "authorization_code";

            var uriBuilder = new UriBuilder(tokenUrl);
            uriBuilder.Query = queryParams.ToString();

            using (var request = new HttpRequestMessage(HttpMethod.Post, uriBuilder.ToString()))
            {
                request.Headers.UserAgent.ParseAdd(userAgent);

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();

                    var contentStream = await response.Content.ReadAsStreamAsync();
                    using(var reader = new StreamReader(contentStream, Encoding.UTF8))
                    {
                        string json = await reader.ReadToEndAsync();

                        return JsonConvert.DeserializeObject<Token>(json);
                    }
                }
            }
        }

        public async Task<Status> GetInnerScanStatus(string accessToken, DateTimeOffset? from)
        {
            var queryParams = HttpUtility.ParseQueryString(String.Empty);
            queryParams["access_token"] = accessToken;
            queryParams["date"] = "1"; // 1: 測定日付
            if (from.HasValue)
            {
                queryParams["from"] = from.Value.ToString("yyyyMMddHHmmss");
            }
            queryParams["tag"] = string.Join(",", Enum.GetValues(typeof(InnerScanTag)).Cast<int>().Select(tag => tag.ToString()));

            var uriBuilder = new UriBuilder(innerScanStatusUrl);
            uriBuilder.Query = queryParams.ToString();

            using (var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString()))
            {
                request.Headers.UserAgent.ParseAdd(userAgent);

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();

                    var contentStream = await response.Content.ReadAsStreamAsync();
                    using (var reader = new StreamReader(contentStream, Encoding.UTF8))
                    {
                        string json = await reader.ReadToEndAsync();

                        return JsonConvert.DeserializeObject<Status>(json);
                    }
                }
            }
        }

        private async Task<string> ScrapeOAuthTokenAsync(string html)
        {
            var parser = new HtmlParser();
            var doc = await parser.ParseAsync(html);

            return doc.All.Where(elem => elem is IHtmlInputElement)
                          .Cast<IHtmlInputElement>()
                          .Where(elem => elem.Name == "oauth_token")
                          .Select(elem => elem.Value)
                          .First();
        }

        private async Task<string> ScrapeCodeAsync(string html)
        {
            var parser = new HtmlParser();
            var doc = await parser.ParseAsync(html);

            return doc.All.Where(elem => elem is IHtmlTextAreaElement)
                          .Cast<IHtmlTextAreaElement>()
                          .Where(elem => elem.Id == "code")
                          .Select(elem => elem.TextContent)
                          .First();
        }
    }
}
