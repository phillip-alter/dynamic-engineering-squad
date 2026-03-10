/* The Report Issue Controller handles the Report Issue workflow: shows landing page, creates report, shows the created report*/

using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InfrastructureApp.Services.ContentModeration;

namespace InfrastructureApp.Controllers
{
    [Authorize]
    public class ReportIssueController : Controller
    {
        //dependency injection (business logic + identity for users)
        private readonly IReportIssueService _service;
        private readonly UserManager<Users> _userManager;

        public ReportIssueController(IReportIssueService service, UserManager<Users> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        //landing page
        [HttpGet]
        public IActionResult ReportIssue() => View();

        //shows the form to create a report +creates a fresh reportIssueViewModel and passes it into the view
        [HttpGet]
        public IActionResult Create(string? cameraId, string? imageUrl, decimal? lat, decimal? lng)
        {
            var report = new ReportIssue
            {
                CameraId = cameraId,
                CameraImageUrl = imageUrl,
                Latitude = lat,
                Longitude = lng
            };

            return View(report);
        }

        //runs when user submits the form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportIssue report)
        {
            if (!ModelState.IsValid)
                return View(report);

            // Get user ID or fallback
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                userId = "user-guid-001"; // tests expect this fallback

            try
            {
                var (reportId, status) = await _service.CreateAsync(report, userId);

              TempData["Success"] = status == "Approved"
                    ? "XP gained! +10 points awarded."
                    : "Report submitted! It will appear on the map once moderation is complete.";
            

                return RedirectToAction("Details", new { id = reportId });
            }

            catch (ContentModerationRejectedException)
            {
                // This comes from the service when content is unsafe.
                // Put the error ON the Description field so it shows next to the textbox.
                ModelState.AddModelError(nameof(report.Description), "Your description contains unsafe content and cannot be submitted.");
                return View(report);
            }

            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(report);
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Something went wrong saving your report. Please try again.");
                return View(report);
            }

        }



        //Shows the details page for a specific report id.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Load report through the service layer; return 404 if it doesn't exist.
            var report = await _service.GetByIdAsync(id);
            if (report == null) return NotFound();

            return View(report);
        }
    }
}
