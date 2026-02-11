using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class ReportIssueRepositoryEf : IReportIssueRepository
    {
        private readonly ApplicationDbContext _db;

        public ReportIssueRepositoryEf(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<ReportIssue>> GetLatestAsync(bool isAdmin)
        {
            var query = _db.ReportIssue.AsQueryable();

            
            query = ReportIssue.VisibleToUser(query, isAdmin);
            query = ReportIssue.OrderLatestFirst(query);

            return await query.ToListAsync();
        }
    }
}
