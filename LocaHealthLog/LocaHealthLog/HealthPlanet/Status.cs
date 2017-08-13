using Newtonsoft.Json;
using System.Collections.Generic;

namespace LocaHealthLog.HealthPlanet
{
    class Status 
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
}
