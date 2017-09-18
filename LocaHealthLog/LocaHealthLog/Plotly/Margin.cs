using Newtonsoft.Json;

namespace LocaHealthLog.Plotly
{
    class Margin
    {
        [JsonProperty("pad")]
        public int? Pad { get; set; }
    }
}
