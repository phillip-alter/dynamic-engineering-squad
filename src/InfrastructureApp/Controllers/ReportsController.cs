using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers
{
    // Controller handles requests for Reports pages
    public class ReportsController : Controller
    {
        // Repository handles all database logic 
        private readonly IReportIssueRepository _repo;

        // Inject repository through constructor (Dependency Injection)
        public ReportsController(IReportIssueRepository repo)
        {
            _repo = repo;
        }

        // GET: /Reports/Latest
        [HttpGet]
        public async Task<IActionResult> Latest()
        {
            bool isAdmin = User.IsInRole("Admin");

            // Repository handles filtering and sorting
            var reports = await _repo.GetLatestReportsAsync(isAdmin);

            // Pass data directly to ViewModel (no copying)
            var vm = new LatestReportsViewModel
            {
                Reports = reports
            };

            return View(vm);
        }

        // -------------------------------------------------------
        // SCRUM-101
        // GET: /Reports/Details/{id}
        // Each report has a unique URL that can be shared
        // -------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            bool isAdmin = User.IsInRole("Admin");

            var report = await _repo.GetByIdAsync(id);

            if (report == null)
            {
                return NotFound();
            }

            // Enforce visibility rule
            if (!isAdmin && report.Status != "Approved")
            {
                return NotFound();
            }

            return View(report);
        }
    }
}


