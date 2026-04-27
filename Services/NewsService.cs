using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace coretex_finalproj.Services
{
    public class NewsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public NewsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["NewsApi:ApiKey"] ?? string.Empty;
        }

        public async Task<string?> GetLiveNewsAsync(string category)
        {
            if (string.IsNullOrEmpty(_apiKey)) return null;

            string url;
            // Focus all queries on "Consumer Electronics" context
            if (category == "economy")
            {
                url = $"https://newsapi.org/v2/everything?q=semiconductor+chip+supply+chain&language=en&pageSize=8&sortBy=publishedAt&apiKey={_apiKey}";
            }
            else if (category == "technology")
            {
                url = $"https://newsapi.org/v2/everything?q=gadgets+smartphone+laptop+wearables&language=en&pageSize=8&sortBy=relevancy&apiKey={_apiKey}";
            }
            else // Default to business
            {
                url = $"https://newsapi.org/v2/everything?q=electronics+hardware+tech+market&language=en&pageSize=8&sortBy=publishedAt&apiKey={_apiKey}";
            }

            try
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "CoretexEDSS");
                var response = await _httpClient.GetStringAsync(url);
                return response;
            }
            catch
            {
                return null;
            }
        }
    }
}
