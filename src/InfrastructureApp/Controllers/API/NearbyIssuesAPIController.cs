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
        private readonly INearbyIssueService _reportQuery;

        public ReportsApiController(INearbyIssueService reportQuery)
        {
            _reportQuery = reportQuery;
        }

        // GET /api/nearbyIssues/nearbyIssues?lat=44.8&lng=-123.2&radiusMiles=5
        [HttpGet("nearbyIssues")]
        public async Task<ActionResult<List<object>>> GetNearby(
            double lat,
            double lng,
            double radiusMiles = 5) //default radius is 5 miles of location
        {
            // Ask service layer to find nearby issues
            var results = await _reportQuery.GetNearbyIssuesAsync(lat, lng, radiusMiles);

            // Convert results into API response objects
            var response = results.Select(r => new
            {
                r.Id,
                r.Status,
                r.CreatedAt,
                r.Latitude,
                r.Longitude,
                r.DistanceMiles,                  // computed distance from user
                DetailsUrl = Url.Action(          // builds URL to MVC Details page
                    "Details",                    //action method
                    "ReportIssue",                //controller
                    new { id = r.Id })!           //route values
            });

            return Ok(response);
        }
    }
}