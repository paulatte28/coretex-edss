using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace coretex_finalproj.Services
{
    public class CurrencyService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<CurrencyService> _logger;

        public CurrencyService(IConfiguration config, ILogger<CurrencyService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            var apiKey = _config["ExchangeRate:ApiKey"];
            var baseUrl = _config["ExchangeRate:BaseUrl"];

            // SIMULATED implementation of Exchange Rate API
            // using var client = new HttpClient();
            // var response = await client.GetAsync($"{baseUrl}{apiKey}/pair/{fromCurrency}/{toCurrency}/{amount}");

            _logger.LogInformation($"[MOCK EXCHANGE RATE] Converting {amount} from {fromCurrency} to {toCurrency}");
            await Task.Delay(50);

            // Mock conversion rate
            decimal rate = 1.0m;
            if (fromCurrency == "USD" && toCurrency == "EUR") rate = 0.9m;
            if (fromCurrency == "EUR" && toCurrency == "USD") rate = 1.1m;
            if (fromCurrency == "PHP" && toCurrency == "USD") rate = 0.018m;
            if (fromCurrency == "USD" && toCurrency == "PHP") rate = 55.0m;

            return amount * rate;
        }
    }
}
