using Newtonsoft.Json;

namespace LocaHealthLog.Plotly
{
    class Image<TX, TY>
    {
        [JsonProperty("figure")]
        public Figure<TX, TY> Figure { get; set; }

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("format")]
        public ImageFormat? Format { get; set; }

        [JsonProperty("scale")]
        public int? Scale { get; set; }

        [JsonProperty("encoded")]
        public bool? Encoded { get; set; }
    }
}
