using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Sidekick.Core.Settings;
using Sidekick.Core.Loggers;

namespace Sidekick.Business.Apis.PoePriceInfo.Models
{
    public class PoePriceInfoClient : IPoePriceInfoClient
    {
        private const string PoePricesBaseUrl = "https://www.poeprices.info/api";
        private readonly ILogger logger;
        private readonly IOptionsMonitor<SidekickSettings> configuration;
        private readonly HttpClient client;

        public PoePriceInfoClient(ILogger logger,
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<SidekickSettings> configuration)
        {
            this.logger = logger;
            this.configuration = configuration;

            client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(PoePricesBaseUrl);
        }

        public async Task<PriceInfoResult> GetItemPricePrediction(string itemText)
        {
            var encodedItem = Convert.ToBase64String(Encoding.UTF8.GetBytes(itemText));

            try
            {
                var response = await client.GetAsync("?l=" + configuration.CurrentValue.LeagueId + "&i=" + encodedItem);
                var content = await response.Content.ReadAsStreamAsync();

                return await JsonSerializer.DeserializeAsync<PriceInfoResult>(content, new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });


            }
            catch
            {
                logger.Log("Error getting price prediction from poeprices.info", LogState.Error);
            }

            return null;
        }
    }
}
