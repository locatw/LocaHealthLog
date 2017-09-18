using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LocaHealthLog.Plotly
{
    class AxisTypeJsonConverter : JsonConverter
    {
        private static readonly Dictionary<AxisType, string> map =
            new Dictionary<AxisType, string>()
            {
                { AxisType.Category, "category" },
                { AxisType.Date, "date" },
                { AxisType.Linear, "linear" },
                { AxisType.Log, "log" },
                { AxisType.None, "-" }
            };

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(AxisType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var axisType = (AxisType)value;

            writer.WriteValue(map[axisType]);
        }
    }
}
