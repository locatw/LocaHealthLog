using Newtonsoft.Json;
using System;

namespace LocaHealthLog.Plotly
{
    class ColorJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Color);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var color = (Color)value;

            writer.WriteValue($"rgb({color.R.ToString()}, {color.G.ToString()}, {color.B.ToString()})");
        }
    }
}
