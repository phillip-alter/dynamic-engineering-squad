/* Contains the core business logic for submitting a report + saving an image + awarding points. */

using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using InfrastructureApp.Services.Moderation;

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
        public async Task<int> CreateAsync(ReportIssueViewModel vm, string userId)
        {
            //hard coded point rule, can change later
            const int pointsForReport = 10;

            // ---------------------------------------------------------
            // 1) Moderation gate (FAIL CLOSED)
            //    Do this BEFORE saving images or touching the DB.
            // ---------------------------------------------------------
            var description = vm.Description ?? string.Empty;

            ModerationResult modResult;
            try
            {
                modResult = await _moderation.CheckAsync(description);
            }
            catch (Exception ex)
            {
                // If moderation service is down / errors out, we do NOT allow submission.
                // This matches your acceptance criteria: "If moderation check fails, report does not submit/save."
                throw new InvalidOperationException(
                    "Moderation service is unavailable. Please try again in a moment.",
                    ex
                );

                // // TEMP: show real reason during debugging                               //for debugging purposes can delete later
                // throw new InvalidOperationException(
                //     $"Moderation failed: {ex.Message}",
                //     ex
                // );
            }

            if (!modResult.IsAllowed)
            {
                throw new ModerationRejectedException(
                    "Your description contains unsafe content and cannot be submitted.",
                    modResult.ReasonCategory
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
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow,
                    UserId = userId
                };

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

                userPoints.CurrentPoints += pointsForReport;
                userPoints.LifetimePoints += pointsForReport;
                userPoints.LastUpdated = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return report.Id;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
