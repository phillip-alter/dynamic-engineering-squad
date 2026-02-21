using InfrastructureApp.Dtos;

namespace InfrastructureApp.Services
{
    public interface INearbyIssueService
    {
        Task<List<NearbyIssueDTO>> GetNearbyIssuesAsync(
            double lat,
            double lng,
            double radiusMiles
        );
    }
}