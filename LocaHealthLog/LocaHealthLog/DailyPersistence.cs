using LocaHealthLog.HealthPlanet;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace LocaHealthLog
{
    public static class DailyPersistence
    {
        private static readonly string userAgent = "loca health log";

        private static HttpClient client = new HttpClient(new HttpClientHandler() { UseCookies = true });

        // start at 01:00 every day.
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

                var api = new Api(userAgent);

                log.Info("Start Log in");
                await api.LoginAsync(loginId, password);

                log.Info("Start authentication");
                string oAuthToken = await api.AuthenticateAsync(clientId);

                log.Info("Start approve");
                string code = await api.ApproveAsync(oAuthToken);

                log.Info("Start get token");
                Token token = await api.GetTokenAsync(clientId, clientSecret, code);

                log.Info("Start get inner scan status");
                Status statsu = await api.GetInnerScanStatus(token.AccessToken);
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

        private static string GetEnvironmentVariable(string key)
        {
            string value = Environment.GetEnvironmentVariable(key);
            if (value != null)
            {
                return value;
            }
            else
            {
                // load environment variables from file if executed at local.
                var secret = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("Secret.json"));
                return secret[key];
            }
        }
    }
}