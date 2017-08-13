using Newtonsoft.Json;

namespace LocaHealthLog.HealthPlanet
{
    class Data
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
