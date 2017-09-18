using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LocaHealthLog.Plotly
{
    class Api
    {
        private static readonly string plotlyClientPlatformHeader = "Plotly-Client-Platform";

        private static readonly string plotlyClientPlatform = "CSharp";

        private static readonly Uri imageUri = new Uri("https://api.plot.ly/v2/images");

        private string userName;

        private string apiKey;

        public Api(string userName, string apiKey)
        {
            this.userName = userName;
            this.apiKey = apiKey;
        }

        public async Task<byte[]> ImagesAsync(string json)
        {
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage { Method = HttpMethod.Post, RequestUri = imageUri })
                {
                    request.Headers.Add(plotlyClientPlatformHeader, plotlyClientPlatform);

                    string authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{apiKey}"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return await response.Content.ReadAsByteArrayAsync();
                    }
                    else
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        throw new Exception(content);
                    }
                }
            }
        }
    }
}
