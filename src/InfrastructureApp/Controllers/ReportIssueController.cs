using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers
{
    public class ReportIssueController : Controller
    {
        private readonly IReportIssueService _reportService;
        private readonly UserManager<Users> _userManager;

        public ReportIssueController(IReportIssueService reportService, UserManager<Users> userManager)
        {
            _reportService = reportService;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ReportIssue() => View();

        [HttpGet]
        public IActionResult Create() => View(new ReportIssueViewModel());

        [HttpPost]
        [AllowAnonymous] // later: [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportIssueViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // Prefer real user if authenticated
            var userId = _userManager.GetUserId(User) ?? "user-guid-001";

            try
            {
                var reportId = await _reportService.CreateAsync(vm, userId);
                TempData["Success"] = "Report submitted! +10 points awarded.";
                return RedirectToAction(nameof(Details), new { id = reportId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }
            catch
            {
                ModelState.AddModelError("", "Something went wrong saving your report. Please try again.");
                return View(vm);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var report = await _reportService.GetByIdAsync(id);
            if (report == null) return NotFound();

            return View(report);
        }
    }
}
