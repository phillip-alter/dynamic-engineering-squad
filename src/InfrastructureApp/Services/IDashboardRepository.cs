using InfrastructureApp.ViewModels;

namespace InfrastructureApp.Services
{
    public interface IDashboardRepository
    {
        Task<DashboardViewModel> GetDashboardSummaryAsync();
    }
}