using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers.API
{
    // API controller used by the Latest Reports search bar.
    // Returns filtered reports as JSON so the page can update using AJAX (no refresh).
    [Route("api/reports")]
    [ApiController]
    public class ReportsAPIController : ControllerBase
    {
        // Repo gives access to ReportIssue data from the database
        private readonly IReportIssueRepository _repo;
        private readonly ILogger<ReportsAPIController> _logger;

        // Constructor with dependency injection
        public ReportsAPIController(IReportIssueRepository repo, ILogger<ReportsAPIController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // GET: /api/reports/latest?query=keyword
        // Called by JavaScript search bar to retrieve filtered Latest Reports
        [HttpGet("latest")]
        public async Task<ActionResult<IEnumerable<object>>> Latest([FromQuery] string? query)
        {
            // Check if current user is Admin (affects which reports are visible)
            bool isAdmin = User.IsInRole("Admin");

            try
            {
                // Get filtered reports from repository
                var reports = await _repo.SearchLatestReportsAsync(isAdmin, query);

                // Return only the fields needed by the UI (security + performance)
                var reportResults = reports.Select(r => new
                {
                    r.Id,
                    r.Description,
                    r.Status,
                    r.ImageUrl,
                    r.CreatedAt // JavaScript will format this date
                });

                // Return results as JSON
                return Ok(reportResults);
            }
            catch (Exception ex)
            {
                
                _logger.LogError(ex, "Error searching latest reports");

                // Return empty list so UI does not crash
                return Ok(Array.Empty<object>());
            }
        }
    }
}