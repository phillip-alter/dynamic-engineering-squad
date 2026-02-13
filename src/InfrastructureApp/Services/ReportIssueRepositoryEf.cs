using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Repositories
{
    public class ReportIssueRepositoryEf : IReportIssueRepository
    {
        private readonly ApplicationDbContext _db;

        public ReportIssueRepositoryEf(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<ReportIssue?> GetByIdAsync(int id)
        {
            return _db.ReportIssue.FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task AddAsync(ReportIssue report)
        {
            _db.ReportIssue.Add(report);
            await Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }
    }
}
