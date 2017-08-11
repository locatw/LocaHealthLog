using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace LocaHealthLog
{
    public static class DailyPersistence
    {
        private static readonly string host = "https://www.healthplanet.jp";

        private static readonly string successUrl = $"{host}/success.html";

        private static readonly string loginUrl = $"{host}/login_oauth.do";

        private static readonly string authUrl = $"{host}/";

        private static readonly string redirectUri = "https://localhost";

        private static readonly string userAgent = "loca health log";

        private static HttpClient client = new HttpClient(new HttpClientHandler() { UseCookies = false });

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