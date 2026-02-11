using InfrastructureApp.Models;

namespace InfrastructureApp.Services
{
    public interface IReportIssueRepository
    {
        // Gets the latest reports
        // isAdmin controls visibility (admins see all, others see approved only)
        Task<List<ReportIssue>> GetLatestAsync(bool isAdmin);
    }
}
