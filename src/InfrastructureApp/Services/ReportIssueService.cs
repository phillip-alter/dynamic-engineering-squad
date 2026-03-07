/* Contains the core business logic for submitting a report + saving an image + awarding points. */

using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using InfrastructureApp.Services.ContentModeration;

namespace InfrastructureApp.Services
{
    public class ReportIssueService : IReportIssueService
    {
        private readonly ApplicationDbContext _db; // for transaction + UserPoints
        private readonly IReportIssueRepository _reports; //repo to insert/fetch ReportIssue records
        private readonly IWebHostEnvironment _env; //gives access to wwwroot so you can save uploaded files
        private readonly IContentModerationService _moderation; //for openAI moderation of user description

        public ReportIssueService(ApplicationDbContext db, IReportIssueRepository reports, IWebHostEnvironment env, IContentModerationService moderation)
        {
            _db = db;
            _reports = reports;
            _env = env;
            _moderation = moderation;
        }

        //service gets delegated to the repo
        public Task<ReportIssue?> GetByIdAsync(int id)
            => _reports.GetByIdAsync(id);

        //submit report workflow
        public async Task<(int reportId, string status)> CreateAsync(ReportIssueViewModel vm, string userId)
        {
            //hard coded point rule, can change later
            const int pointsForReport = 10;

            // ---------------------------------------------------------
            // 1) Moderation gate (FAIL CLOSED)
            //    Do this BEFORE saving images or touching the DB.
            // ---------------------------------------------------------
            var description = vm.Description ?? string.Empty;

            // ContentModerationService should now return Performed=false instead of throwing
            var modResult = await _moderation.CheckAsync(description);

            Console.WriteLine($"[ReportIssue] ModerationResult: Performed={modResult.Performed}, IsAllowed={modResult.IsAllowed}, Flagged={modResult.Flagged}, Reason={modResult.Reason ?? "(none)"}");

            if (!modResult.Performed)
            {
                // Moderation didn't actually run (startup/network/429/etc).
                // FAIL SAFE: do not publish publicly.
                // We will still accept the submission but keep it hidden until reviewed.
            }
            else if (!modResult.IsAllowed)
            {
                // Moderation ran and flagged content
                throw new ContentModerationRejectedException(
                    "Your description contains unsafe content and cannot be submitted.",
                    modResult.Reason
                );
            }

            // ---------------------------------------------------------
            // 2) Save file (local) — only after moderation is SAFE
            // ---------------------------------------------------------
            string? savedImagePath = null;

            if (vm.Photo != null && vm.Photo.Length > 0)
            {
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(vm.Photo.FileName).ToLowerInvariant();

                //validates extensions
                if (!allowedExts.Contains(ext))
                    throw new InvalidOperationException("Only JPG, PNG, or WEBP images are allowed.");

                //validates image size
                const long maxBytes = 5 * 1024 * 1024;
                if (vm.Photo.Length > maxBytes)
                    throw new InvalidOperationException("Image must be 5MB or smaller.");

                //save folder for images
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "issues");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(uploadsFolder, fileName);

                //save the image
                using (var stream = File.Create(fullPath))
                {
                    await vm.Photo.CopyToAsync(stream);
                }

                savedImagePath = $"/uploads/issues/{fileName}";
            }


            //starts database transaction, saves it if everything completes otherwise gives an error
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var report = new ReportIssue
                {
                    Description = vm.Description,
                    Latitude = vm.Latitude,
                    Longitude = vm.Longitude,
                    ImageUrl = savedImagePath,
                    Status = modResult.Performed ? "Approved" : "Pending",
                    CreatedAt = DateTime.UtcNow,
                    UserId = userId
                };

                Console.WriteLine($"[ReportIssue] Saving report with Status={report.Status}");

                await _reports.AddAsync(report);
                await _reports.SaveChangesAsync(); // report.Id now populated

                // user points update (still EF, but in service layer)
                var userPoints = await _db.UserPoints.FirstOrDefaultAsync(up => up.UserId == userId);
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

                if (modResult.Performed)
                {
                    userPoints.CurrentPoints += pointsForReport;
                    userPoints.LifetimePoints += pointsForReport;
                    userPoints.LastUpdated = DateTime.UtcNow;
                }
                
                // One save no matter what (report + maybe points)
                await _db.SaveChangesAsync();
                
                await tx.CommitAsync();

                return (report.Id, report.Status);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
