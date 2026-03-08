using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers.API
{
    // API controller used by the Latest Reports page.
    // Supports AJAX features like search, sort, and modal detail loading.
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

        // GET: /api/reports/latest?query=keyword&sort=newest
        // Called by JavaScript search/sort on the Latest Reports page
        [HttpGet("latest")]
        public async Task<ActionResult<IEnumerable<object>>> Latest([FromQuery] string? query, [FromQuery] string? sort) // SCRUM-86 UPDATED: added sort parameter
        {
            // Check if current user is Admin (affects which reports are visible)
            bool isAdmin = User.IsInRole("Admin");

            try
            {
                // Get filtered reports from repository
                var reports = await _repo.SearchLatestReportsAsync(isAdmin, query, sort);

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

        // GET: /api/reports/5
        // SCRUM-98 ADDED: returns one report's details for the popup modal, including map coordinates
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetReportById([FromRoute] int id)
        {
            bool isAdmin = User.IsInRole("Admin");

            try
            {
                var report = await _repo.GetByIdAsync(id);

                if (report == null)
                {
                    return NotFound();
                }

                // Non-admin users should not see non-approved reports
                if (!isAdmin && report.Status != "Approved")
                {
                    return NotFound();
                }

                // Return the full details needed by the popup modal
                return Ok(new
                {
                    report.Id,
                    report.Description,
                    report.Status,
                    report.ImageUrl,
                    report.CreatedAt,
                    report.Latitude,
                    report.Longitude
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report details for report id {ReportId}", id);
                return NotFound();
            }
        }
    }
}