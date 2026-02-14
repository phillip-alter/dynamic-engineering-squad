using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class DashboardRepositoryEf : IDashboardRepository
    {
        private readonly ApplicationDbContext _db;

        public DashboardRepositoryEf(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardViewModel> GetDashboardSummaryAsync()
        {
            // Load a demo/seeded user if one exists.
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return BuildPlaceholderDashboard();
            }

            return await BuildDashboardForUserAsync(user);
        }

        private static DashboardViewModel BuildPlaceholderDashboard()
        {
            return new DashboardViewModel
            {
                Username = "DemoUser",
                Email = "demo@example.com",
                ReportsSubmitted = 0,
                Points = 0
            };
        }

        private async Task<DashboardViewModel> BuildDashboardForUserAsync(Users user)
        {
            // Count reports submitted by this user
            var reportsSubmitted = await _db.ReportIssue
                .AsNoTracking()
                .CountAsync(r => r.UserId == user.Id);

            // Load points if they exist
            var pointsRow = await _db.UserPoints
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            return new DashboardViewModel
            {
                Username = user.UserName ?? "DemoUser",
                Email = user.Email ?? "demo@example.com",
                ReportsSubmitted = reportsSubmitted,
                Points = pointsRow?.CurrentPoints ?? 0
            };
        }
    }
}