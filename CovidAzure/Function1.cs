using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CovidAzure
{
    public static class Function1
    {
        private const string Schedule = "0 0 13-18 * * *";
        //private const string Schedule = "0 * * * * *";

        private static HttpClient GovClient;

        [FunctionName("Function1")]
        public async static Task Run([TimerTrigger(Schedule)]TimerInfo myTimer, ILogger log)
        {
            SetupGovClient();
            
            var homeUpdater = new HomeAssistantUpdater(log);

            var englandUrl = @"/v1/data?filters=areaType=nation;areaName=england&structure={""date"":""date"",""new"":""newCasesByPublishDate"",""total"":""cumCasesByPublishDate""}";
            var townUrl = @"/v1/data?filters=areaName=peterborough&structure={""date"":""date"",""new"":""newCasesByPublishDate"",""total"":""cumCasesByPublishDate""}";
            
            Task.WaitAll(
                GetData(homeUpdater, "england", englandUrl, log),
                GetData(homeUpdater, "peterborough", townUrl, log)
            );

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }

        private static void SetupGovClient()
        {
            if (GovClient == null)
            {
                var clientHandler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                GovClient = new HttpClient(clientHandler)
                {
                    BaseAddress = new Uri("https://api.coronavirus.data.gov.uk"),
                };
            }
        }

        private static async Task GetData(HomeAssistantUpdater homeAssistantUpdater, string key, string url, ILogger log)
        {
            var response = await GovClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var covid = await response.Content.ReadFromJsonAsync<Covid>();
                var sevenDayAverage = covid.Data.Take(7).Average(c => c.New);

                Task.WaitAll(
                    homeAssistantUpdater.Update($"{key}_new", covid.Data.First().New),
                    homeAssistantUpdater.Update($"{key}_total", covid.Data.First().Total.Value),
                    homeAssistantUpdater.Update($"{key}_average", sevenDayAverage)
                );
            }
            else
            {
                log.LogError("Call failed with bad http status code", url, response.StatusCode);
            }
        }
    }
}
