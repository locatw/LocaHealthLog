using LocaHealthLog.HealthPlanet;
using LocaHealthLog.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
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
                var appConfig = AppConfig.Load();

                var storageClient = new StorageClient();
                log.Info("Start connect to table storage");
                await storageClient.ConnectAsync(appConfig.StorageConnectionString);

                var now = DateTimeOffset.UtcNow;
                var acquisitionPeriod = new TimeSpan(7, 0, 0, 0);
                var end = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0,
                                             AppConfig.LocalTimeZone.BaseUtcOffset);
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
    }
}
