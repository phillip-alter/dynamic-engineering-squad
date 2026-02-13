using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class ReportIssueService : IReportIssueService
    {
        private readonly ApplicationDbContext _db; // for transaction + UserPoints
        private readonly IReportIssueRepository _reports;
        private readonly IWebHostEnvironment _env;

        public ReportIssueService(ApplicationDbContext db, IReportIssueRepository reports, IWebHostEnvironment env)
        {
            _db = db;
            _reports = reports;
            _env = env;
        }

        public Task<ReportIssue?> GetByIdAsync(int id)
            => _reports.GetByIdAsync(id);

        public async Task<int> CreateAsync(ReportIssueViewModel vm, string userId)
        {
            const int pointsForReport = 10;

            // Save file (local)
            string? savedImagePath = null;

            if (vm.Photo != null && vm.Photo.Length > 0)
            {
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(vm.Photo.FileName).ToLowerInvariant();

                if (!allowedExts.Contains(ext))
                    throw new InvalidOperationException("Only JPG, PNG, or WEBP images are allowed.");

                const long maxBytes = 5 * 1024 * 1024;
                if (vm.Photo.Length > maxBytes)
                    throw new InvalidOperationException("Image must be 5MB or smaller.");

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "issues");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = File.Create(fullPath))
                {
                    await vm.Photo.CopyToAsync(stream);
                }

                savedImagePath = $"/uploads/issues/{fileName}";
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
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
