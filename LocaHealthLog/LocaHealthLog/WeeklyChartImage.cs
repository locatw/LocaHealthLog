using LocaHealthLog.HealthPlanet;
using LocaHealthLog.Plotly;
using LocaHealthLog.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LocaHealthLog
{
    public static class WeeklyChartImage
    {
        // start on Saturday at 07:00 JST.
        [FunctionName("WeeklyChartImage")]
        public static async Task Run([TimerTrigger("0 0 22 * * 5")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"Start WeeklyChartImage at: {DateTime.Now}");

            try
            {
                var appConfig = AppConfig.Load();

                var storageClient = new StorageClient();
                log.Info("Start connect to table storage");
                await storageClient.ConnectAsync(appConfig.StorageConnectionString);

                var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
                var begin = new DateTimeOffset(yesterday.Year, yesterday.Month, 1, 0, 0, 0, AppConfig.LocalTimeZone.BaseUtcOffset);
                var end = begin.AddMonths(1);
                IEnumerable<WeightEntity> monthlyWeights =
                    storageClient.LoadMeasurementData(InnerScanTag.Weight.ToString(), begin, end)
                        .Cast<WeightEntity>();

                Image<int, double> chartImage = MakeMonthlyWeightChart(yesterday.Year, yesterday.Month, monthlyWeights);
                string imageJson = GenerateChartImageJson(chartImage);

                var plotlyApi = new Plotly.Api(appConfig.PlotlyUserName, appConfig.PlotlyApiKey);
                byte[] image = await plotlyApi.ImagesAsync(imageJson);
                using (var writer = new System.IO.FileStream("result.png", System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    await writer.WriteAsync(image, 0, image.Length);
                }
            }
            catch (Exception e)
            {
                log.Error($"Exception: {e.ToString()}");
            }
            finally
            {
                log.Info($"Finish WeeklyChartImage at: {DateTime.Now}");
            }
        }

        private static Image<int, double> MakeMonthlyWeightChart(int year, int month, IEnumerable<WeightEntity> data)
        {
            Color backgroundColor = new Color { R = 200, G = 200, B = 200 };
            Color gridColor = new Color { R = 180, G = 180, B = 180 };

            DateTimeOffset startDay = new DateTimeOffset(year, month, 1, 0, 0, 0, AppConfig.LocalTimeZone.BaseUtcOffset);
            DateTimeOffset endDay = new DateTimeOffset(year, month + 1, 1, 0, 0, 0, AppConfig.LocalTimeZone.BaseUtcOffset).AddDays(-1);
            List<DateTime> monthDays = new List<DateTime>();
            for (var day = startDay.Date; day.Date <= endDay.Date; day = day.AddDays(1))
            {
                monthDays.Add(day);
            }

            IEnumerable<double> weights = data.Select(entity => entity.Weight);
            double minWeight = weights.Min();
            double maxWeight = weights.Max();

            return new Image<int, double>
            {
                Figure = new Figure<int, double>
                {
                    Data = new List<Plotly.Data<double>>()
                    {
                        new Plotly.Data<double>
                        {
                            Y = weights
                        }
                    },
                    Layout = new Layout<int, double>
                    {
                        Margin = new Margin()
                        {
                            Pad = 3
                        },
                        PlotBackgroundColor = backgroundColor,
                        PaperBackgroundColor = backgroundColor,
                        ShowLegend = false,
                        Title = "ëÃèdÅikgÅj",
                        XAxis = new Axis<int>
                        {
                            GridColor = gridColor,
                            Range = new int[] { -1, monthDays.Count - 1 },
                            TickMode = "array",
                            TickText = monthDays.Select(day => day.ToString("MM/dd")).Where((_, i) => i % 2 == 0),
                            TickValues = monthDays.Select(day => Convert.ToInt32(day.ToString("dd")) - 1).Where((_, i) => i % 2 == 0),
                            Type = AxisType.None,
                            ZeroLine = false,
                        },
                        YAxis = new Axis<double>
                        {
                            DTick = 0.5,
                            GridColor = gridColor,
                            Range = new double[] { Math.Floor(minWeight - 0.5), Math.Ceiling(maxWeight + 0.5) },
                            TickFormat = ".2f",
                            Type = AxisType.Linear,
                            ZeroLine = false,
                        }
                    }
                },
                Width = 800,
                Height = 500,
                Format = ImageFormat.Png,
                Scale = 1,
                Encoded = false
            };
        }

        private static string GenerateChartImageJson(Image<int, double> chartImage)
        {
            var jsonSerializeSettings = new Newtonsoft.Json.JsonSerializerSettings()
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(chartImage, jsonSerializeSettings);
        }
    }
}
