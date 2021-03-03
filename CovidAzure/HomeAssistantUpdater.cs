using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CovidAzure
{
    class HomeAssistantUpdater
    {
        private static HttpClient HomeClient;

        private ILogger _log;

        public HomeAssistantUpdater(ILogger log)
        {
            SetupClient();
            _log = log;
        }

        public async Task Update(string key, object value)
        {
            var url = $"/api/states/sensor.covid_{key.ToLower().Replace(' ', '_')}";

            try
            {
                var homeResponse = await HomeClient.GetAsync(url);
                if (homeResponse.IsSuccessStatusCode)
                {
                    var homeEntity = await homeResponse.Content.ReadFromJsonAsync<Entity>();

                    if (homeEntity.Last_changed.Date == DateTime.Today && homeEntity.State == value.ToString())
                    {
                        // Already been updated today, so no need to continue.
                        return;
                    }
                }
                else if (homeResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    _log.LogError("Call failed with bad http status code", url, homeResponse.StatusCode);
                }

            }
            catch (Exception ex)
            {
                _log.LogError("Call failed with bad http status code", url, ex);
            }

            var data = new
            {
                state = value,
                attributes = new
                {
                    friendly_name = $"{key} cases",
                    unit_of_measurement = "people",
                    icon = "mdi:virus-outline"
                }
            };

            await PostUpdate(url, data);
        }

        public async Task UpdateLastRun()
        {
            var url = $"/api/states/sensor.covid_last_ran";

            var data = new
            {
                state = DateTime.UtcNow,
                attributes = new
                {
                    friendly_name = $"Last ran",
                    unit_of_measurement = "time",
                    icon = "mdi:virus-outline"
                }
            };

            await PostUpdate(url, data);
        }

        private async Task PostUpdate(string url, object data)
        {
            var response = await HomeClient.PostAsJsonAsync(url, data);
            
            if (!response.IsSuccessStatusCode)
            {
                _log.LogError("Unable to update", url, response.StatusCode);
            }
        }

        private void SetupClient()
        {
            if (HomeClient == null)
            {
                HomeClient = new HttpClient()
                {
                    BaseAddress = new Uri(Environment.GetEnvironmentVariable("HomeAssistantUrl")),
                };
                
                HomeClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("HAToken")}");
            }
        }
    }
}
