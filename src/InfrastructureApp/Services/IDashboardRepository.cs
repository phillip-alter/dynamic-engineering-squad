using InfrastructureApp.ViewModels;

namespace InfrastructureApp.Services
{
    // Repository interface for loading dashboard data
    public interface IDashboardRepository
    {
        // Returns the data needed to display the Dashboard page
        Task<DashboardViewModel> GetDashboardSummaryAsync();

        // Returns public profile data for any user by username (no email)
        Task<DashboardViewModel?> GetPublicProfileAsync(string username);
    }
}