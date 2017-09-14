using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace LocaHealthLog
{
    class AppConfig
    {
        private static readonly string LocalSecretFileName = "Secret.json";

        public static readonly TimeZoneInfo LocalTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

        public static AppConfig Load()
        {
            return new AppConfig()
            {
                ClientId = GetEnvironmentVariable("ClientId"),
                ClientSecret = GetEnvironmentVariable("ClientSecret"),
                LoginId = GetEnvironmentVariable("LoginId"),
                Password = GetEnvironmentVariable("Password"),
                StorageConnectionString = GetEnvironmentVariable("StorageConnectionString")
            };
        }

        public string ClientId { get; private set; }

        public string ClientSecret { get; private set; }

        public string LoginId { get; private set; }

        public string Password { get; private set; }

        public string StorageConnectionString { get; private set; }

        private AppConfig()
        { }

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
                var secret = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                                File.ReadAllText(LocalSecretFileName));
                return secret[key];
            }
        }
    }
}
