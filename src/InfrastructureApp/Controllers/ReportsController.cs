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
            // Latest page shows approved reports by default; admins can see all
            bool isAdmin = User.IsInRole("Admin");

            // Delegate database access and filtering to the repository to keep the controller thin (separation of concerns)
            var reports = await _repo.GetLatestReportsAsync(isAdmin);  //TODO: Look and check for this part. 

            // Convert domain models → ViewModels for the UI
            var items = reports.Select(r => new LatestReportItemViewModel //TODO: Look and check for this part. 
            {
                Id = r.Id,
                Description = r.Description,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            }).ToList();

            // Put list into page ViewModel
            var vm = new LatestReportsViewModel
            {
                Reports = items
            };

            // Send data to the view (Razor Page)
            return View(vm);
        }
    }
}


