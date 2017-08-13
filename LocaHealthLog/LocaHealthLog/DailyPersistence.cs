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

        private static readonly string tokenUrl = $"{host}/oauth/token";

        private static readonly string innerScanStatusUrl = $"{host}/status/innerscan.json";

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
                Token token = await GetTokenAsync(log, clientId, clientSecret, code);
                Status status = await GetInnerScanStatus(log, token.AccessToken);
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

        private static async Task<Token> GetTokenAsync(TraceWriter log, string clientId, string clientSecret, string code)
        {
            log.Info("Start get token");

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
                    log.Info($"Get token response status: {response.StatusCode.ToString()}");

                    var contentStream = await response.Content.ReadAsStreamAsync();
                    using(var reader = new StreamReader(contentStream, Encoding.UTF8))
                    {
                        string json = await reader.ReadToEndAsync();

                        return JsonConvert.DeserializeObject<Token>(json);
                    }
                }
            }
        }

        private static async Task<Status> GetInnerScanStatus(TraceWriter log, string accessToken)
        {
            log.Info("Start get inner scan status");

            var queryParams = HttpUtility.ParseQueryString(String.Empty);
            queryParams["access_token"] = accessToken;
            queryParams["date"] = "1"; // 1: 測定日付
            queryParams["from"] = DateTime.Today.AddDays(-1).ToString("yyyyMMddHHmmss");
            queryParams["tag"] = string.Join(",", Enum.GetValues(typeof(InnerScanTag)).Cast<int>().Select(tag => tag.ToString()));

            var uriBuilder = new UriBuilder(innerScanStatusUrl);
            uriBuilder.Query = queryParams.ToString();

            using (var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString()))
            {
                request.Headers.UserAgent.ParseAdd(userAgent);

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    log.Info($"Get inner scan status response status: {response.StatusCode.ToString()}");

                    var contentStream = await response.Content.ReadAsStreamAsync();
                    using (var reader = new StreamReader(contentStream, Encoding.UTF8))
                    {
                        string json = await reader.ReadToEndAsync();

                        return JsonConvert.DeserializeObject<Status>(json);
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

        enum InnerScanTag
        {
            // 体重(kg)
            Weight = 6021,
            // 体脂肪率(%)
            BodyFatPercentage = 6022,
            // 筋肉量(kg)
            MuscleMass = 6023,
            // 筋肉スコア
            MuscleScore = 6024,
            // 内臓脂肪レベル2
            VisceralFatLevel2 = 6025,
            // 内臓脂肪レベル
            VesceralFatLevel = 6026,
            // 基礎代謝量(kcal)
            BasalMetabolicRate = 6027,
            // 体内年齢(才)
            BodyAge = 6028,
            // 推定骨量(kg)
            EstimatedBoneMass = 6029
        }

        class Token
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("expires_in")]
            public string ExpiresIn { get; set; }

            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
        }

        public class Status 
        {
            [JsonProperty("birth_date")]
            public string BirthDate { get; set; }

            [JsonProperty("height")]
            public string Height { get; set; }

            [JsonProperty("sex")]
            public string Sex { get; set; }

            [JsonProperty("data")]
            public List<Data> Data { get; set; }
        }

        public class Data
        {
            [JsonProperty("date")]
            public string Date { get; set; }

            [JsonProperty("keydata")]
            public string KeyData { get; set; }

            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("tag")]
            public string Tag { get; set; }
        }
    }
}