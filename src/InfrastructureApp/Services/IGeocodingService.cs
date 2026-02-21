namespace InfrastructureApp.Services
{
    public interface IGeocodingService
    {
        Task<(double lat, double lng)> GeocodeAsync(string address);
    }
}