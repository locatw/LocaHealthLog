using Newtonsoft.Json;

namespace LocaHealthLog.Plotly
{
    [JsonConverter(typeof(ColorJsonConverter))]
    struct Color
    {
        public int R { get; set; }

        public int G { get; set; }

        public int B { get; set; }
    }
}
