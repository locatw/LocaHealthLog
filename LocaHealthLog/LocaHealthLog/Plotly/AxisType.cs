using Newtonsoft.Json;

namespace LocaHealthLog.Plotly
{
    [JsonConverter(typeof(AxisTypeJsonConverter))]
    enum AxisType
    {
        Category,
        Date,
        Linear,
        Log,
        None
    }
}
