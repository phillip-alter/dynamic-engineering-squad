using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class ReportIssueRepositoryEf : IReportIssueRepository
    {
        private readonly ApplicationDbContext _db;

        // Inject DbContext through DI
        public ReportIssueRepositoryEf(ApplicationDbContext db)
        {
            _db = db;
        }

        // Returns latest reports based on visibility rules
        public async Task<List<ReportIssue>> GetLatestReportsAsync(bool isAdmin)
        {
            // Start building query from ReportIssue table
            var query = _db.ReportIssue.AsQueryable();

            // Apply visibility filtering (approved vs all)
            query = ReportIssue.VisibleToUser(query, isAdmin);

            // Sort newest reports first
            query = ReportIssue.OrderLatestFirst(query);

            // Execute query and return results
            return await query.ToListAsync();
        }
    }
}
