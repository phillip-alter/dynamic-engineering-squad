using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Controllers
{
    //require login to submit reports
    //can allow anonymous
    //[Authorize]
    public class ReportIssueController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<Users> _userManager;

        //your identity user type is "Users"
        public ReportIssueController(ApplicationDbContext db, UserManager<Users> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        //optional public landing page
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ReportIssue()
        {
            return View();
        }

        //GET: /ReportIssue/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new ReportIssueViewModel());
        }


        [HttpPost]
        [AllowAnonymous] // testing; later switch to [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportIssueViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            const int pointsForReport = 10;

            // TEST: use seeded user (palter)
            var userId = "user-guid-001";

            // Keep report + points consistent
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                // 1) Save the report
                var report = new ReportIssue
                {
                    Description = vm.Description,
                    Latitude = vm.Latitude,
                    Longitude = vm.Longitude,
                    ImageUrl = string.IsNullOrWhiteSpace(vm.ImageUrl) ? null : vm.ImageUrl.Trim(),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    UserId = userId
                };

                _db.ReportIssue.Add(report);
                await _db.SaveChangesAsync(); // report.Id is now generated

                // 2) Update/create UserPoints for THIS SAME userId
                var userPoints = await _db.UserPoints
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (userPoints == null)
                {
                    userPoints = new UserPoints
                    {
                        UserId = userId,
                        CurrentPoints = 0,
                        LifetimePoints = 0,
                        LastUpdated = DateTime.UtcNow
                    };

                    _db.UserPoints.Add(userPoints);
                }

                userPoints.CurrentPoints += pointsForReport;
                userPoints.LifetimePoints += pointsForReport;
                userPoints.LastUpdated = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                await tx.CommitAsync();

                TempData["Success"] = $"Report submitted! +{pointsForReport} points awarded.";
                return RedirectToAction(nameof(Details), new { id = report.Id });
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError("", "Something went wrong saving your report. Please try again.");
                return View(vm);
            }
        }

        //GET ReportIssue/Details/5
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var report = await _db.ReportIssue.FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            return View(report);
        }
    }
}