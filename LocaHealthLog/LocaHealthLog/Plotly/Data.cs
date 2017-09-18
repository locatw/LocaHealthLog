using Newtonsoft.Json;
using System.Collections.Generic;

namespace LocaHealthLog.Plotly
{
    class Data<TY>
    {
        [JsonProperty("y")]
        public IEnumerable<TY> Y { get; set; }
    }
}
