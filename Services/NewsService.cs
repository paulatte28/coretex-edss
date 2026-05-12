using System;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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
            
            // Set User-Agent once here to avoid duplicate header exceptions
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "CoretexEDSS/1.0");
            }
        }

        public async Task<string?> GetLiveNewsAsync(string category)
        {
            if (string.IsNullOrEmpty(_apiKey)) return null;

            string query;
            // Broaden queries to ensure we always get results for the CEO
            switch (category.ToLower())
            {
                case "economy":
                    query = "global+economy+inflation+market+trends";
                    break;
                case "technology":
                    query = "tech+innovation+gadgets+AI+future";
                    break;
                case "business":
                    query = "corporate+strategy+startup+finance";
                    break;
                case "pricing":
                    query = "electronics+prices+inflation+market+forecast";
                    break;
                default:
                    query = "tech+business+market+trends";
                    break;
            }

            // Using "everything" but with a filter to keep it relevant to business/tech
            // Increase to 20 results for a fuller feed
            string url = $"https://newsapi.org/v2/everything?q={query}&language=en&pageSize=20&sortBy=relevancy&apiKey={_apiKey}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                
                // Log failure status if needed
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
