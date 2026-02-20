using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class DashboardRepositoryEf : IDashboardRepository
    {
        
        private readonly ApplicationDbContext _db; // Database context used for queries

        public DashboardRepositoryEf(ApplicationDbContext db)
        {
            _db = db;
        }


        // Attempt to load a seeded/demo user from the database.
        // If none exists, return placeholder values so the dashboard
        // remains functional until full account integration is enabled.
        public async Task<DashboardViewModel> GetDashboardSummaryAsync() // Builds the dashboard summary for the current user
        {
            
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync();

            // If no user exists, return default placeholder values
            if (user == null)
            {
                return BuildPlaceholderDashboard();
            }

            // Otherwise build dashboard using real data
            return await BuildDashboardForUserAsync(user);
        }

        // Returns default dashboard values when no user is found
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

        // Builds dashboard values using database data
        private async Task<DashboardViewModel> BuildDashboardForUserAsync(Users user)
        {
            // Count how many reports this user submitted
            var reportsSubmitted = await _db.ReportIssue
                .AsNoTracking()
                .CountAsync(r => r.UserId == user.Id);

            // Get user points (if they exist)
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