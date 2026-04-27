using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace coretex_finalproj.Services
{
    public class TrendService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public TrendService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["SerpApi:ApiKey"] ?? string.Empty;
            _baseUrl = configuration["SerpApi:BaseUrl"] ?? "https://serpapi.com/search.json";
        }

        public async Task<string?> GetMarketTrendsAsync(string query = "Laptops, Smartphones, Tablets")
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("YOUR_")) return null;

            // engine=google_trends is the key here
            // geo=PH limits the data to the Philippines
            var url = $"{_baseUrl}?engine=google_trends&q={query}&geo=PH&api_key={_apiKey}";

            try
            {
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
