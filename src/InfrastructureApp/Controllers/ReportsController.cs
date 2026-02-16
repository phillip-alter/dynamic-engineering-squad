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
    }
}


