using InfrastructureApp.Models;

namespace InfrastructureApp.Services
{
    public interface IReportIssueRepository
    {
        Task<List<ReportIssue>> GetLatestAsync(bool isAdmin);
    }
}
