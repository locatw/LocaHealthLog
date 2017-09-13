using LocaHealthLog.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LocaHealthLog
{
    public static class WeeklyChartImage
    {
        // start on Saturday at 07:00 JST.
        [FunctionName("WeeklyChartImage")]
        public static async Task Run([TimerTrigger("0 0 22 * * 5")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"Start WeeklyChartImage at: {DateTime.Now}");

            try
            {
            }
            catch (Exception e)
            {
                log.Error($"Exception: {e.ToString()}");
            }
            finally
            {
                log.Info($"Finish WeeklyChartImage at: {DateTime.Now}");
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

        private static string LoadStorageConnectionString()
        {
            string value = Environment.GetEnvironmentVariable("StorageConnectionString");
            if (value != null)
            {
                return value;
            }
            else
            {
                var secret = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("Secret.json"));
                return secret["StorageConnectionString"];
            }
        }
    }
}
