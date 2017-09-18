using Newtonsoft.Json;
using System.Collections.Generic;

namespace LocaHealthLog.Plotly
{
    class Axis<T>
    {
        [JsonProperty("dtick")]
        public double? DTick { get; set; } = null;

        [JsonProperty("gridcolor")]
        public Color? GridColor { get; set; }

        [JsonProperty("range")]
        public IEnumerable<T> Range { get; set; }

        [JsonProperty("tickformat")]
        public string TickFormat { get; set; }

        [JsonProperty("tickmode")]
        public string TickMode { get; set; }

        [JsonProperty("ticktext")]
        public IEnumerable<string> TickText { get; set; }

        [JsonProperty("tickvals")]
        public IEnumerable<int> TickValues { get; set; }

        [JsonProperty("type")]
        public AxisType Type { get; set; }

        [JsonProperty("zeroline")]
        public bool? ZeroLine { get; set; }
    }
}
