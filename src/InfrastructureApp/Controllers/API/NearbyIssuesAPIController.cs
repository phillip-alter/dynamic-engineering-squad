//This API endpoint answers What infrastructure issues exist near this location is what this API controller does.

using InfrastructureApp.Dtos;     //data transfer objects used between layers
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers.Api
{
    [ApiController]
    [Route("api/nearbyIssues")]
    public class ReportsApiController : ControllerBase
    {
        // Service responsible for querying nearby reports
        private readonly INearbyIssueService _nearbyReport;

        public ReportsApiController(INearbyIssueService nearbyReport)
        {
            _nearbyReport = nearbyReport;
        }

        // GET /api/nearbyIssues/nearbyIssues?lat=44.8&lng=-123.2&radiusMiles=5
        [HttpGet("nearbyIssues")]
        public async Task<ActionResult<IReadOnlyList<NearbyIssueDTO>>> GetNearby([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radiusMiles = 5) //default radius is 5 miles of location
        {
            // Ask service layer to find nearby issues
            var results = await _nearbyReport.GetNearbyIssuesAsync(lat, lng, radiusMiles);
            return Ok(results);
        }
    }
}