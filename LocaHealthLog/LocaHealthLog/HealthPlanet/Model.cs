using System;

namespace LocaHealthLog.HealthPlanet
{
    enum Model
    {
        ManualInputed,
        BC501,
        BC502,
        BC503,
        MC180,
        MC190,
        DC320,
        WB510,
        BC504,
        BC567,
        BC569,
        BC308,
        BC309,
        SC330,
        MC980
    }

    static class ModelExtension
    {
        public static string ToInteger(this Model model)
        {
            switch (model)
            {
                case Model.ManualInputed: return "00000000";
                case Model.BC501: return "01000001";
                case Model.BC502: return "01000002";
                case Model.BC503: return "01000003";
                case Model.MC180: return "01000022";
                case Model.MC190: return "01000023";
                case Model.DC320: return "01000024";
                case Model.WB510: return "01000067";
                case Model.BC504: return "01000072";
                case Model.BC567: return "01000074";
                case Model.BC569: return "01000075";
                case Model.BC308: return "01000076";
                case Model.BC309: return "01000077";
                case Model.SC330: return "01000079";
                case Model.MC980: return "01000080";
                default: throw new NotImplementedException($"Not supported model: {model.ToString()}");
            }
        }
    }

    static class ModelHelper
    {
        public static Model MakeFromIntegerString(string integerValue)
        {
            switch (integerValue)
            {
                case "00000000": return Model.ManualInputed;
                case "01000001": return Model.BC501;
                case "01000002": return Model.BC502;
                case "01000003": return Model.BC503;
                case "01000022": return Model.MC180;
                case "01000023": return Model.MC190;
                case "01000024": return Model.DC320;
                case "01000067": return Model.WB510;
                case "01000072": return Model.BC504;
                case "01000074": return Model.BC567;
                case "01000075": return Model.BC569;
                case "01000076": return Model.BC308;
                case "01000077": return Model.BC309;
                case "01000079": return Model.SC330;
                case "01000080": return Model.MC980;
                default: throw new ArgumentException($"Invalid model value: {integerValue}");
            }
        }
    }
}
