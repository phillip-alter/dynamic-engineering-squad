using System.Text.Json;

namespace InfrastructureApp.Services
{
    public class GeocodingService : IGeocodingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public GeocodingService(
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<(double lat, double lng)> GeocodeAsync(string address)
        {
            var key = _config["GoogleMaps:GeocodingApiKey"];

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Geocoding key not configured.");

            var url =
                $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&region=us&key={Uri.EscapeDataString(key)}";

            var client = _httpClientFactory.CreateClient();
            var json = await client.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var status = root.GetProperty("status").GetString();

            if (status != "OK")
            {
                var errorMessage =
                    root.TryGetProperty("error_message", out var em)
                        ? em.GetString()
                        : "Unknown geocoding error.";

                throw new Exception($"Google Geocode failed: {status} - {errorMessage}");
            }

            var loc = root
                .GetProperty("results")[0]
                .GetProperty("geometry")
                .GetProperty("location");

            return (
                loc.GetProperty("lat").GetDouble(),
                loc.GetProperty("lng").GetDouble()
            );
        }
    }
}