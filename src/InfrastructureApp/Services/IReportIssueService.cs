using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;

namespace InfrastructureApp.Services
{
    public interface IReportIssueService
    {
        Task<int> CreateAsync(ReportIssueViewModel vm, string userId);
        Task<ReportIssue?> GetByIdAsync(int id);
    }
}
