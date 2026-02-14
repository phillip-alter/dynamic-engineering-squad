using InfrastructureApp.Data;
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
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync();

            
            string username = "DemoUser";
            string email = "demo@example.com";
            int reportsSubmitted = 0;
            int points = 0;

            if (user != null)
            {
                username = user.UserName ?? username;
                email = user.Email ?? email;

                reportsSubmitted = await _db.ReportIssue
                    .AsNoTracking()
                    .CountAsync(r => r.UserId == user.Id);

                var pointsRow = await _db.UserPoints
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                points = pointsRow?.CurrentPoints ?? 0;
            }

            return new DashboardViewModel
            {
                Username = username,
                Email = email,
                ReportsSubmitted = reportsSubmitted,
                Points = points
            };
        }
    }
}