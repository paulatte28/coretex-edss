using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace coretex_finalproj.Services
{
    public class ExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExchangeRateService> _logger;

        public ExchangeRateService(HttpClient httpClient, IConfiguration configuration, ILogger<ExchangeRateService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<decimal> ConvertToPhpAsync(string fromCurrency, decimal amount)
        {
            if (fromCurrency.ToUpper() == "PHP") return amount;

            try
            {
                var apiKey = _configuration["ExchangeRateApi:ApiKey"];
                var baseUrl = _configuration["ExchangeRateApi:BaseUrl"];
                
                // Format: https://v6.exchangerate-api.com/v6/YOUR-API-KEY/pair/FROM/TO/AMOUNT
                var url = $"{baseUrl}{apiKey}/pair/{fromCurrency.ToUpper()}/PHP/{amount}";

                var response = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>(url);

                if (response != null && response.result == "success")
                {
                    _logger.LogInformation($"Successfully converted {amount} {fromCurrency} to {response.conversion_result} PHP");
                    return (decimal)response.conversion_result;
                }

                _logger.LogWarning($"Exchange Rate API returned failure for {fromCurrency}");
                return amount; // Fallback to original amount if API fails
            }
            catch (Exception ex)
            {
                _logger.LogError($"Currency Conversion Error: {ex.Message}");
                return amount; // Fallback to original amount
            }
        }

        private class ExchangeRateResponse
        {
            public string result { get; set; }
            public double conversion_result { get; set; }
        }
    }
}
