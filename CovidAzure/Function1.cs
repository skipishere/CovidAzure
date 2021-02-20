using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CovidAzure
{
    public static class Function1
    {
        //private const string Schedule = "0 */5 * * * *";
        private const string Schedule = "0 * * * * *";

        private static HttpClient Client;

        [FunctionName("Function1")]
        public async static Task Run([TimerTrigger(Schedule)]TimerInfo myTimer, ILogger log)
        {
            if (Client == null)
            {
                var clientHandler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                Client = new HttpClient(clientHandler)
                {
                    BaseAddress = new Uri("https://api.coronavirus.data.gov.uk"),
                };
            }

            var url = @"/v1/data?filters=areaType=nation;areaName=england&structure={""date"":""date"",""new"":""newCasesByPublishDate"",""total"":""cumCasesByPublishDate""}";

            var response = await Client.GetStringAsync(url);



            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
