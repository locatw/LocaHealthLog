using Newtonsoft.Json;
using System.Collections.Generic;

namespace LocaHealthLog.Plotly
{
    class Figure<TX, TY>
    {
        [JsonProperty("data")]
        public IEnumerable<Data<TY>> Data { get; set; }

        [JsonProperty("layout")]
        public Layout<TX, TY> Layout { get; set; }
    }
}
