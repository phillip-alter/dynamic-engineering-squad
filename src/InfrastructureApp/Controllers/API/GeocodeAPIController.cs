using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers.Api
{
    [ApiController]
    [Route("api/geocode")]
    public class GeocodeApiController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public GeocodeApiController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> Geocode([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Missing query parameter 'q'." });

            var key = _config["GoogleMaps:GeocodingApiKey"];
            if (string.IsNullOrWhiteSpace(key))
                return StatusCode(500, new { message = "Server geocoding key not configured." });

            var url =
                $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(q)}&region=us&key={Uri.EscapeDataString(key)}";

            var client = _httpClientFactory.CreateClient();
            var json = await client.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var status = root.GetProperty("status").GetString();

            if (status != "OK")
            {
                var errorMessage = root.TryGetProperty("error_message", out var em) ? em.GetString() : null;
                return BadRequest(new { status, errorMessage });
            }

            var loc = root
                .GetProperty("results")[0]
                .GetProperty("geometry")
                .GetProperty("location");

            var lat = loc.GetProperty("lat").GetDouble();
            var lng = loc.GetProperty("lng").GetDouble();

            return Ok(new { lat, lng });
        }
    }
}