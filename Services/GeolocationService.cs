using System.Net.Http.Json;

namespace coretex_finalproj.Services
{
    public class GeolocationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public GeolocationService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<GeoLocationResult> GetLocationAsync(string ipAddress)
        {
            try
            {
                // SPECIAL AUTO-DETECT: If on localhost, find the developer's REAL public IP
                if (ipAddress == "::1" || ipAddress == "127.0.0.1")
                {
                    try {
                        ipAddress = await _httpClient.GetStringAsync("https://api.ipify.org");
                    } catch {
                        ipAddress = "112.198.115.11"; // Fallback to a PH IP if detection fails
                    }
                }

                var apiKey = _config["IpGeolocation:ApiKey"];
                var baseUrl = _config["IpGeolocation:BaseUrl"];
                
                var response = await _httpClient.GetFromJsonAsync<GeoLocationResult>($"{baseUrl}?apiKey={apiKey}&ip={ipAddress}");
                return response ?? new GeoLocationResult();
            }
            catch
            {
                return new GeoLocationResult { City = "Unknown", CountryName = "Unknown", Isp = "Unknown" };
            }
        }
    }

    public class GeoLocationResult
    {
        [System.Text.Json.Serialization.JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("country_name")]
        public string CountryName { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("isp")]
        public string Isp { get; set; } = string.Empty;
    }
}
