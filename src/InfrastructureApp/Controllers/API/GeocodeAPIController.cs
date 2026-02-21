//This API endpoint converts address to latitude + longitude.

using InfrastructureApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers.Api
{
    [ApiController]
    [Route("api/geocode")]
    public class GeocodeApiController : ControllerBase
    {
        private readonly IGeocodingService _geocodingService;

        public GeocodeApiController(IGeocodingService geocodingService)
        {
            _geocodingService = geocodingService;
        }

        [HttpGet]
        public async Task<IActionResult> Geocode([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Missing query parameter 'q'." });

            try
            {
                var (lat, lng) = await _geocodingService.GeocodeAsync(q);

                return Ok(new { lat, lng });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}