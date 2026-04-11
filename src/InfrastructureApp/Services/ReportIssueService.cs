/* Contains the core business logic for submitting a report + saving an image + awarding points. */

/**The logic for image moderation should be:
save image first
build image URL
set SeverityStatus = Pending
call moderation
if moderation passes, call severity estimator
if severity succeeds, overwrite Pending
if anything fails, leave as Pending
save report anyway **/

using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;
using InfrastructureApp.Services.ContentModeration;
using InfrastructureApp.Services.ImageHashing;
using InfrastructureApp.Services.ImageSeverity;

namespace InfrastructureApp.Services
{
    public class ReportIssueService : IReportIssueService
    {
        private readonly ApplicationDbContext _db; // for transaction + UserPoints
        private readonly IReportIssueRepository _reports; //repo to insert/fetch ReportIssue records
        private readonly IWebHostEnvironment _env; //gives access to wwwroot so you can save uploaded files
        private readonly IContentModerationService _moderation; //for openAI moderation of user description
        private readonly IImageHashService _imageHashService; // computes SHA-256 + pHash for duplicate image detection

        private readonly IImageModerationService _imageModerationService; //image severity moderation
        private readonly IImageSeverityEstimationService _imageSeverityEstimationService; //image severity estimation
        private readonly IHttpContextAccessor _httpContextAccessor; //image severity

        public ReportIssueService(ApplicationDbContext db, IReportIssueRepository reports, IWebHostEnvironment env, IContentModerationService moderation, 
                                    IImageHashService imageHashService, IImageModerationService imageModerationService, IImageSeverityEstimationService imageSeverityEstimationService,
                                    IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _reports = reports;
            _env = env;
            _moderation = moderation;
            _imageHashService = imageHashService;
            _imageModerationService = imageModerationService;
            _imageSeverityEstimationService = imageSeverityEstimationService;
            _httpContextAccessor = httpContextAccessor;

        }

        //service gets delegated to the repo
        public Task<ReportIssue?> GetByIdAsync(int id)
            => _reports.GetByIdAsync(id);

        //used during report creation when processing uploaded images
        private string? BuildAbsoluteImageUrl(string relativePath)
{
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{relativePath}";
}

        //submit report workflow
        //create a report, moderate it, validate image, hash image, reject duplicates, save image, award points
        public async Task<(int reportId, string status)> CreateAsync(ReportIssue report, string userId)
        {
            //hard coded point rule, can change later
            const int pointsForReport = 10;

            // ---------------------------------------------------------
            // 1) Moderation gate (FAIL CLOSED)
            //    Do this BEFORE saving images or touching the DB.
            // ---------------------------------------------------------
            var description = report.Description ?? string.Empty;

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
            // 2) Validate image + compute hashes + reject duplicates
            // ---------------------------------------------------------

            string? savedImagePath = null;

            if (report.Photo != null && report.Photo.Length > 0)
            {
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(report.Photo.FileName).ToLowerInvariant();

                // Validate extension
                if (!allowedExts.Contains(ext))
                    throw new InvalidOperationException("Only JPG, PNG, or WEBP images are allowed.");

                // Validate size
                const long maxBytes = 5 * 1024 * 1024;
                if (report.Photo.Length > maxBytes)
                    throw new InvalidOperationException("Image must be 5MB or smaller.");

                // -----------------------------------------------------
                // Compute BOTH hashes from the uploaded image
                // BEFORE saving the file to disk.
                // -----------------------------------------------------
                await using var uploadStream = report.Photo.OpenReadStream();
                ImageHashResult hashes = await _imageHashService.ComputeHashesAsync(uploadStream);

                // -----------------------------------------------------
                // EXACT duplicate check:
                // same user + same SHA-256 = same exact file
                // -----------------------------------------------------
                bool exactDuplicateExists = await _db.ReportIssue
                    .AnyAsync(r =>
                        r.UserId == userId &&
                        r.ImageSha256 == hashes.Sha256);

                if (exactDuplicateExists)
                {
                    throw new DuplicateImageException(
                        "You already used this image in a previous report. Please upload a different image.");
                }

                // -----------------------------------------------------
                // VISUAL similarity check:
                // compare new pHash against the same user's earlier pHashes
                // -----------------------------------------------------
                List<long> priorPHashes = await _db.ReportIssue
                    .Where(r => r.UserId == userId && r.ImagePHash.HasValue)
                    .Select(r => r.ImagePHash!.Value)
                    .ToListAsync();

                // Starting threshold:
                // smaller = stricter
                // larger = more tolerant
                //
                // 8 is a practical starting point.
                const int pHashThreshold = 8;

                foreach (long previousHash in priorPHashes)
                {
                    int distance = _imageHashService.HammingDistance(hashes.PHash, previousHash);

                    if (distance <= pHashThreshold)
                    {
                        throw new DuplicateImageException(
                            "This image looks too similar to one you already uploaded. Please use a different image.");
                    }
                }

                // -----------------------------------------------------
                // Save file only AFTER duplicate/moderation checks pass
                // -----------------------------------------------------
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "issues");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = File.Create(fullPath))
                {
                    await report.Photo.CopyToAsync(stream);
                }

                savedImagePath = $"/uploads/issues/{fileName}";

                // -----------------------------------------------------
                // 3) Image severity estimation (AI)
                // -----------------------------------------------------

                report.SeverityStatus = ImageSeverityStatuses.Pending;

                // Build absolute URL for OpenAI
                var absoluteImageUrl = BuildAbsoluteImageUrl(savedImagePath);
                Console.WriteLine($"[Severity] savedImagePath = {savedImagePath}");
                Console.WriteLine($"[Severity] absoluteImageUrl = {absoluteImageUrl ?? "(null)"}");

                if (!string.IsNullOrWhiteSpace(absoluteImageUrl))
                {
                    var moderationResult = await _imageModerationService.ModerateImageAsync(absoluteImageUrl);

                    Console.WriteLine(
                        $"[Severity] ImageModeration: Performed={moderationResult.Performed}, " +
                        $"IsViable={moderationResult.IsViable}, Reason={moderationResult.Reason ?? "(none)"}");

                    if (moderationResult.Performed && moderationResult.IsViable)
                    {
                        var severityResult = await _imageSeverityEstimationService
                            .EstimateSeverityAsync(absoluteImageUrl);

                        Console.WriteLine(
                            $"[Severity] SeverityEstimate: Performed={severityResult.Performed}, " +
                            $"Severity={severityResult.SeverityStatus}, Reason={severityResult.Reason ?? "(none)"}");


                        if (severityResult.Performed)
                        {
                            report.SeverityStatus = severityResult.SeverityStatus;
                        }
                        else
                        {
                            Console.WriteLine("[Severity] Skipped because absoluteImageUrl was null or empty.");
                        }
                    }
                }

                // Save the hashes onto the report row so we can compare future uploads.
                report.ImageSha256 = hashes.Sha256;
                report.ImagePHash = hashes.PHash;
            }

            //starts database transaction, saves it if everything completes otherwise gives an error
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                report.ImageUrl = savedImagePath ?? report.CameraImageUrl;
                report.Status = modResult.Performed ? "Approved" : "Pending";
                report.CreatedAt = DateTime.UtcNow;
                report.UserId = userId;

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
