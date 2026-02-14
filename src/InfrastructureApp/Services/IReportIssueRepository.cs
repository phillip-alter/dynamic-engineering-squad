using InfrastructureApp.Models;

namespace InfrastructureApp.Services
{
    public interface IReportIssueRepository
    {
        // Existing basic repo operations (used by other report features)
        Task<ReportIssue?> GetByIdAsync(int id);
        Task AddAsync(ReportIssue report);
        Task SaveChangesAsync();

        // Latest Reports feature
        // isAdmin controls visibility (admins see all, others see approved only)
        Task<List<ReportIssue>> GetLatestReportsAsync(bool isAdmin);
    }
}