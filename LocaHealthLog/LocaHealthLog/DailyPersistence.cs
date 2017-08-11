using AngleSharp.Parser.Html;
using AngleSharp.Dom.Html;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace LocaHealthLog
{
    public static class DailyPersistence
    {
        private static readonly string host = "https://www.healthplanet.jp";

        private static readonly string successUrl = $"{host}/success.html";

        private static readonly string loginUrl = $"{host}/login_oauth.do";

        private static readonly string authUrl = $"{host}/oauth/auth";

        private static readonly string approveUrl = $"{host}/oauth/approval.do";

        private static readonly string redirectUri = "https://localhost";

        private static readonly string userAgent = "loca health log";

        private static HttpClient client = new HttpClient(new HttpClientHandler() { UseCookies = true });

        // 毎日01:00に起動する。
        [FunctionName("DailyPersistence")]
        public static async void Run([TimerTrigger("0 0 1 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"Start DailyPersistence at: {DateTime.Now}");

            try
            {
                string clientId = GetEnvironmentVariable("ClientId");
                string clientSecret = GetEnvironmentVariable("ClientSecret");
                string loginId = GetEnvironmentVariable("LoginId");
                string password = GetEnvironmentVariable("Password");

                await LoginAsync(log, loginId, password);
                string oAuthToken = await AuthenticateAsync(log, clientId);
                string code = await ApproveAsync(log, oAuthToken);
            }
            catch (Exception e)
            {
                log.Info($"Exception: {e.ToString()}");
            }
            finally
            {
                log.Info($"Finish DailyPersistence at: {DateTime.Now}");
            }
        }

        private static async Task LoginAsync(TraceWriter log, string loginId, string password)
        {
            log.Info($"Log in to {loginUrl}");

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
                    log.Info($"Log in response status: {response.StatusCode.ToString()}");
                }
            }
        }

        private static async Task<string> AuthenticateAsync(TraceWriter log, string clientId)
        {
            log.Info("Start authentication");

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
                    log.Info($"Authentication response status: {response.StatusCode.ToString()}");

                    var contentStream = await response.Content.ReadAsStreamAsync();
                    using(var reader = new StreamReader(contentStream, Encoding.GetEncoding("Shift_JIS")))
                    {
                        string html = await reader.ReadToEndAsync();

                        return await ScrapeOAuthTokenAsync(html);
                    }
                }
            }
        }

        private static async Task<string> ApproveAsync(TraceWriter log, string oAuthToken)
        {
            log.Info("Start approve");

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
                    log.Info($"Approve response status: {response.StatusCode.ToString()}");

                    var contentStream = await response.Content.ReadAsStreamAsync();
                    using(var reader = new StreamReader(contentStream, Encoding.GetEncoding("Shift_JIS")))
                    {
                        string html = await reader.ReadToEndAsync();

                        return await ScrapeCodeAsync(html);
                    }
                }
            }
        }

        private static async Task<string> ScrapeOAuthTokenAsync(string html)
        {
            var parser = new HtmlParser();
            var doc = await parser.ParseAsync(html);

            return doc.All.Where(elem => elem is IHtmlInputElement)
                          .Cast<IHtmlInputElement>()
                          .Where(elem => elem.Name == "oauth_token")
                          .Select(elem => elem.Value)
                          .First();
        }

        private static async Task<string> ScrapeCodeAsync(string html)
        {
            var parser = new HtmlParser();
            var doc = await parser.ParseAsync(html);

            return doc.All.Where(elem => elem is IHtmlTextAreaElement)
                          .Cast<IHtmlTextAreaElement>()
                          .Where(elem => elem.Id == "code")
                          .Select(elem => elem.TextContent)
                          .First();
        }

        private static string GetEnvironmentVariable(string key)
        {
            string value = Environment.GetEnvironmentVariable(key);
            if (value != null)
            {
                return value;
            }
            else
            {
                // ローカルで実行するときはファイルから環境変数を読み込む。
                var secret = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("Secret.json"));
                return secret[key];
            }
        }
    }
}