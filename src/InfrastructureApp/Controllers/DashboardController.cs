using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers
{
    // Handles requests for the Dashboard page
    [Authorize]
    public class DashboardController : Controller
    {
        // Repository used to load dashboard data
        private readonly IDashboardRepository _dashboardRepo;

        // Inject repository through dependency injection
        public DashboardController(IDashboardRepository dashboardRepo)
        {
            _dashboardRepo = dashboardRepo;
        }

        // GET: /Dashboard
        // If username is provided, shows that user's dashboard; otherwise shows the logged-in user's own dashboard.
        [HttpGet]
        public async Task<IActionResult> Index(string? username = null)
        {
            DashboardViewModel vm;

            if (!string.IsNullOrWhiteSpace(username))
            {
                var profile = await _dashboardRepo.GetPublicProfileAsync(username);
                if (profile == null) return NotFound();
                vm = profile;
            }
            else
            {
                vm = await _dashboardRepo.GetDashboardSummaryAsync();
            }

            return View(vm);
        }
    }
}