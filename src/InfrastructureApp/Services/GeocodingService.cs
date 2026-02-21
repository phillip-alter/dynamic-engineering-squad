//Given an address, what GPS coordinates does Google Maps return?

using System.Text.Json;     // Used for parsing JSON responses returned by Google Maps API

namespace InfrastructureApp.Services
{
    // Concrete implementation of IGeocodingService
    // Responsible ONLY for converting addresses to coordinates
    public class GeocodingService : IGeocodingService
    {
        // Factory for safely creating HttpClient instances
        private readonly IHttpClientFactory _httpClientFactory;

        // Used to read configuration values such as API keys
        private readonly IConfiguration _config;

        public GeocodingService(
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // Converts an address into latitude + longitude
        public async Task<(double lat, double lng)> GeocodeAsync(string address)
        {
            // Read API key from configuration
            var key = _config["GoogleMaps:GeocodingApiKey"];

            // If key missing then server configuration problem
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Geocoding key not configured.");

            // Build Google Geocoding API request URL
            var url =
                $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&region=us&key={Uri.EscapeDataString(key)}";

            // Create HTTP client using factory
            var client = _httpClientFactory.CreateClient();

            // Send request to Google Maps API
            // Response comes back as JSON text
            var json = await client.GetStringAsync(url);

            // Parse JSON response
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Read Google API status field
            var status = root.GetProperty("status").GetString();

            // If Google API failed
            if (status != "OK")
            {
                // Try to read optional Google error message
                var errorMessage =
                    root.TryGetProperty("error_message", out var em)
                        ? em.GetString()
                        : "Unknown geocoding error.";

                // Throw exception so controller can handle it
                throw new Exception($"Google Geocode failed: {status} - {errorMessage}");
            }

            // Navigate JSON structure:
            var loc = root
                .GetProperty("results")[0]
                .GetProperty("geometry")
                .GetProperty("location");

            // Return latitude + longitude as a tuple
            return (
                loc.GetProperty("lat").GetDouble(),
                loc.GetProperty("lng").GetDouble()
            );
        }
    }
}