using InfrastructureApp.Dtos;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers.Api
{
    [ApiController]
    [Route("api/nearby")]
    public class ReportsApiController : ControllerBase
    {
        private readonly INearbyIssueService _reportQuery;

        public ReportsApiController(INearbyIssueService reportQuery)
        {
            _reportQuery = reportQuery;
        }

        [HttpGet("nearby")]
        public async Task<ActionResult<List<object>>> GetNearby(
            double lat,
            double lng,
            double radiusMiles = 5)
        {
            var results = await _reportQuery.GetNearbyIssuesAsync(lat, lng, radiusMiles);

            var response = results.Select(r => new
            {
                r.Id,
                r.Status,
                r.CreatedAt,
                r.Latitude,
                r.Longitude,
                r.DistanceMiles,
                DetailsUrl = Url.Action(
                    "Details",
                    "ReportIssue",
                    new { id = r.Id })!
            });

            return Ok(response);
        }
    }
}