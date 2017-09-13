using LocaHealthLog.HealthPlanet;
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
        private static readonly TimeZoneInfo jstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

        // start on Saturday at 07:00 JST.
        [FunctionName("WeeklyChartImage")]
        public static async Task Run([TimerTrigger("0 0 22 * * 5")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"Start WeeklyChartImage at: {DateTime.Now}");

            try
            {
                var storageClient = new StorageClient();
                log.Info("Start connect to table storage");
                await storageClient.ConnectAsync(LoadStorageConnectionString());

                var now = DateTimeOffset.UtcNow;
                var acquisitionPeriod = new TimeSpan(7, 0, 0, 0);
                var end = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, jstTimeZone.BaseUtcOffset);
                var begin = end - acquisitionPeriod;
                IEnumerable<InnerScanStatusEntity> last7DatesScanData =
                    storageClient.LoadMeasurementData(InnerScanTag.Weight.ToString(), begin, end);
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
