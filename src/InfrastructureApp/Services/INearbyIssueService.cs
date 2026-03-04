//Given a location, what infrastructure issues exist nearby?

using InfrastructureApp.Dtos;     // Import DTO objects used for transferring data safely

namespace InfrastructureApp.Services
{
    public interface INearbyIssueService
    {
        // Asynchronously retrieves infrastructure issues located
        // within a specified radius of a latitude/longitude.
        // RETURNS:
        // A list of NearbyIssueDTO objects containing
        // map-ready information (coordinates, status, distance, etc.)
        // var issues = await service.GetNearbyIssuesAsync(44.84, -123.23, 5);
        Task<List<NearbyIssueDTO>> GetNearbyIssuesAsync(double lat, double lng, double radiusMiles);
    }
}