using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CovidAzure
{
    public static class Function1
    {
#if DEBUG
        private const string Schedule = "0 * * * * *";
#else
        private const string Schedule = "0 0 16-20 * * *";
#endif

        private static HttpClient GovClient;

        [FunctionName("Function1")]
        public static void Run([TimerTrigger(Schedule)]TimerInfo myTimer, ILogger log)
        {
            SetupGovClient();
            
            var homeUpdater = new HomeAssistantUpdater(log);
            var town = Environment.GetEnvironmentVariable("Town");
            const string structure = @"{""date"":""date"",""new"":""newCasesByPublishDate"",""total"":""cumCasesByPublishDate"",""firstDosePercentage"":""cumVaccinationFirstDoseUptakeByPublishDatePercentage"",""secondDosePercentage"":""cumVaccinationSecondDoseUptakeByPublishDatePercentage"",""thirdDosePercentage"":""cumVaccinationThirdInjectionUptakeByPublishDatePercentage""}";

            var ukUrl = $"/v1/data?filters=areaType=overview&structure={structure}";
            var englandUrl = $"/v1/data?filters=areaType=nation;areaName=england&structure={structure}";
            var townUrl = $"/v1/data?filters=areaType=ltla;areaName={town.ToLower()}&structure={structure}";
            
            Task.WaitAll(
                GetData(homeUpdater, "UK", ukUrl, log),
                GetData(homeUpdater, "England", englandUrl, log),
                GetData(homeUpdater, town, townUrl, log),
                homeUpdater.UpdateLastRun()
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
                var sevenDayAverage = Math.Round(covid.Data.Take(7).Average(c => c.New), 1);
                var latest = covid.Data.First();
                var vaccineData = covid.Data.FirstOrDefault(c => c.FirstDosePercentage.HasValue && c.SecondDosePercentage.HasValue);

                Task.WaitAll(
                    homeAssistantUpdater.Update($"{key} new", latest.New),
                    homeAssistantUpdater.Update($"{key} total", latest.Total.Value),
                    homeAssistantUpdater.Update($"{key} average", sevenDayAverage),
                    homeAssistantUpdater.UpdateVaccine($"{key} 1st vaccine", vaccineData?.FirstDosePercentage),
                    homeAssistantUpdater.UpdateVaccine($"{key} 2nd vaccine", vaccineData?.SecondDosePercentage),
                    homeAssistantUpdater.UpdateVaccine($"{key} 3rd vaccine", vaccineData?.ThirdDosePercentage)
                );
            }
            else
            {
                log.LogError("Call failed with bad http status code", url, response.StatusCode);
            }
        }
    }
}
