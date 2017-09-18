using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LocaHealthLog.Plotly
{
    [JsonConverter(typeof(StringEnumConverter), true)]
    enum ImageFormat
    {
        Eps,
        Jpeg,
        Pdf,
        Png,
        Svg,
        Webp
    }
}
