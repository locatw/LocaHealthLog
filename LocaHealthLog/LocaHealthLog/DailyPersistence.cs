using LocaHealthLog.HealthPlanet;
using LocaHealthLog.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace LocaHealthLog
{
    public static class DailyPersistence
    {
        private static readonly string userAgent = "loca health log";

        private static readonly TimeZoneInfo jstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

        // start at 01:00 JST every day.
        [FunctionName("DailyPersistence")]
        public static async Task Run([TimerTrigger("0 0 16 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"Start DailyPersistence at: {DateTime.Now}");

            try
            {
                var storageClient = new StorageClient();
                log.Info("Start connect to table storage");
                await storageClient.ConnectAsync(LoadStorageConnectionString());

                var lastMeasurementDate = storageClient.LoadLastMeasurementDate();
                DateTimeOffset? from;
                if (lastMeasurementDate.HasValue)
                {
                    log.Info($"Last measurement date: {lastMeasurementDate.Value.ToString()}");
                    from = lastMeasurementDate.Value.ToOffset(jstTimeZone.BaseUtcOffset);
                }
                else
                {
                    log.Info("No measurement");
                    from = null;
                }

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
                Status status = await api.GetInnerScanStatus(token.AccessToken, from);

                log.Info("Start inserting entities to storage");
                var entityFactory = new EntityFactory();
                IEnumerable<InnerScanStatusEntity> entities = entityFactory.MakeFrom(status);
                await storageClient.BatchInsertAsync(entities);

                DateTimeOffset measurementDate = entities.Select(entity => entity.MeasurementDate).Max();
                if (!lastMeasurementDate.HasValue || lastMeasurementDate.Value < measurementDate)
                {
                    log.Info("Start updating last measurement date");
                    await storageClient.UpdateLastMeasurementDate(measurementDate);
                }
            }
            catch (Exception e)
            {
                log.Error($"Exception: {e.ToString()}");
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