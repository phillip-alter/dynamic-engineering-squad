using System.Text.Json; //// Used to parse JSON returned from Google Maps API
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers.Api
{
    [ApiController]
    [Route("api/geocode")]
    public class GeocodeApiController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;   // Factory used to create HttpClient safely
        private readonly IConfiguration _config;   // Used to read configuration values, (appsettings.json, user-secrets, environment variables)

        public GeocodeApiController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // GET /api/geocode?q=some address
        [HttpGet]
        public async Task<IActionResult> Geocode([FromQuery] string q)
        {
            // Validate query parameter
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Missing query parameter 'q'." });

            // Read Google Maps API key from configuration
            var key = _config["GoogleMaps:GeocodingApiKey"];

            // If key missing then server misconfiguration
            if (string.IsNullOrWhiteSpace(key))
                return StatusCode(500, new { message = "Server geocoding key not configured." });

            // Build Google Geocoding API request URL
            // Uri.EscapeDataString protects against invalid characters
            var url =
                $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(q)}&region=us&key={Uri.EscapeDataString(key)}";

            // Create HttpClient using factory
            var client = _httpClientFactory.CreateClient();

            // Send HTTP request to Google API
            // Returns JSON string response
            var json = await client.GetStringAsync(url);

            // Parse returned JSON
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extract API status
            var status = root.GetProperty("status").GetString();

            // If Google didn't return success
            if (status != "OK")
            {
                // Try to read optional Google error message
                var errorMessage = root.TryGetProperty("error_message", out var em) ? em.GetString() : null;

                // Return failure to frontend
                return BadRequest(new { status, errorMessage });
            }

            // Navigate JSON structure:
            var loc = root
                .GetProperty("results")[0]
                .GetProperty("geometry")
                .GetProperty("location");

            // Extract latitude + longitude
            var lat = loc.GetProperty("lat").GetDouble();
            var lng = loc.GetProperty("lng").GetDouble();

            // Return only coordinates to frontend
            return Ok(new { lat, lng });
        }
    }
}