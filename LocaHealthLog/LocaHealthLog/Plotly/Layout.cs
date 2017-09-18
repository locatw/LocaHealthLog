using Newtonsoft.Json;

namespace LocaHealthLog.Plotly
{
    class Layout<TX, TY>
    {
        [JsonProperty("margin")]
        public Margin Margin { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("xaxis")]
        public Axis<TX> XAxis { get; set; }

        [JsonProperty("yaxis")]
        public Axis<TY> YAxis { get; set; }

        [JsonProperty("plot_bgcolor")]
        public Color? PlotBackgroundColor { get; set; }

        [JsonProperty("paper_bgcolor")]
        public Color? PaperBackgroundColor { get; set; }

        [JsonProperty("showlegend")]
        public bool? ShowLegend { get; set; }
    }
}
