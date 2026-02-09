using InfrastructureApp.Data;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReportsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Reports/Latest
        [HttpGet]
        public async Task<IActionResult> Latest()
        {
            // If the ReportIssue table/entity exists already, use it.
            // Otherwise, use placeholder data (see the fallback below).

            var items = await _db.ReportIssue
                .OrderByDescending(r => r.CreatedAt)   // most recent first
                .Select(r => new LatestReportItemViewModel
                {
                    Id = r.Id,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var vm = new LatestReportsViewModel
            {
                Reports = items
            };

            return View(vm);
        }
    }
}