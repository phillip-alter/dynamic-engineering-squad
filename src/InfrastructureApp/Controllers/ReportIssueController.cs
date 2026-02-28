/* The Report Issue Controller handles the Report Issue workflow: shows landing page, creates report, shows the created report*/

using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InfrastructureApp.Services.Moderation;

namespace InfrastructureApp.Controllers
{
    public class ReportIssueController : Controller
    {
        //dependency injection (business logic + identity for users)
        private readonly IReportIssueService _reportService;
        private readonly UserManager<Users> _userManager;

        public ReportIssueController(IReportIssueService reportService, UserManager<Users> userManager)
        {
            _reportService = reportService;
            _userManager = userManager;
        }

        //landing page
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ReportIssue() => View();

        //shows the form to create a report +creates a fresh reportIssueViewModel and passes it into the view
        [HttpGet]
        public IActionResult Create() => View(new ReportIssueViewModel());

        //runs when user submits the form
        [HttpPost]
        [AllowAnonymous] // later: [Authorize for users]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportIssueViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // Prefer real user if authenticated, testing user-guid-001 for now
            var userId = _userManager.GetUserId(User) ?? "user-guid-001";

            try
            {
                //creating report
                var reportId = await _reportService.CreateAsync(vm, userId);
                TempData["Success"] = "XP gained! +10 points awarded.";
                return RedirectToAction(nameof(Details), new { id = reportId });
            }
            catch (ModerationRejectedException)
            {
                // This comes from the service when content is unsafe.
                // Put the error ON the Description field so it shows next to the textbox.
                ModelState.AddModelError(nameof(vm.Description), "Your description contains unsafe content and cannot be submitted.");
                return View(vm);
            }
            catch (InvalidOperationException ex)
            {
                //catch errors (missing photo, duplicate image, coordinate)
                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }
            catch
            {
                //catch unexpected errors
                ModelState.AddModelError("", "Something went wrong saving your report. Please try again.");
                return View(vm);
            }
        }

        //Shows the details page for a specific report id.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            // Load report through the service layer; return 404 if it doesn't exist.
            var report = await _reportService.GetByIdAsync(id);
            if (report == null) return NotFound();

            return View(report);
        }
    }
}
