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

        private readonly IWebHostEnvironment _env;

        //your identity user type is "Users"
        public ReportIssueController(ApplicationDbContext db, UserManager<Users> userManager, IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
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

            // Save file (optional)
            string? savedImagePath = null;

            if(vm.Photo != null && vm.Photo.Length > 0)
            {
                // Basic extension validation (add more later)
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(vm.Photo.FileName).ToLowerInvariant();

                if (!allowedExts.Contains(ext))
                {
                    ModelState.AddModelError(nameof(vm.Photo), "Only JPG, PNG, or WEBP images are allowed.");
                    return View(vm);
                }

                // Optional size limit example (5 MB)
                const long maxBytes = 5 * 1024 * 1024;
                if (vm.Photo.Length > maxBytes)
                {
                    ModelState.AddModelError(nameof(vm.Photo), "Image must be 5MB or smaller.");
                    return View(vm);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "issues");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = System.IO.File.Create(fullPath))
                {
                    await vm.Photo.CopyToAsync(stream);
                }

                savedImagePath = $"/uploads/issues/{fileName}";
            }

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
                    ImageUrl = savedImagePath,
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