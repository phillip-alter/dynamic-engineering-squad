using InfrastructureApp.Models;

namespace InfrastructureApp.Repositories
{
    public interface IReportIssueRepository
    {
        Task<ReportIssue?> GetByIdAsync(int id);
        Task AddAsync(ReportIssue report);
        Task SaveChangesAsync();
    }
}
