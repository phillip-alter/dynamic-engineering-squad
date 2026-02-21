//This API endpoint converts address to latitude + longitude.

using InfrastructureApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers.Api
{
    [ApiController]
    [Route("api/geocode")]
    public class GeocodeApiController : ControllerBase
    {
        // Service responsible for geocoding logic
        private readonly IGeocodingService _geocodingService;

        public GeocodeApiController(IGeocodingService geocodingService)
        {
            _geocodingService = geocodingService;
        }

        // GET /api/geocode?q=some address
        [HttpGet]
        public async Task<IActionResult> Geocode([FromQuery] string q)
        {
            // Validate query parameter
            // Prevent empty or missing address
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Missing query parameter 'q'." });

            try
            {
                // Call service layer to perform geocoding
                var (lat, lng) = await _geocodingService.GeocodeAsync(q);

                return Ok(new { lat, lng });
            }
            catch (Exception ex)
            {
                // If service throws error (API failure, bad address, etc.)
                // return readable error to frontend
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}