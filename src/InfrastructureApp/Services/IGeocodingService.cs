//Any geocoding service must be able to convert an address into coordinates.

namespace InfrastructureApp.Services
{
    public interface IGeocodingService
    {
        // Asynchronously converts an address into
        // geographic coordinates (latitude + longitude).
        // var (lat, lng) = await GeocodeAsync("Salem Oregon");
        Task<(double lat, double lng)> GeocodeAsync(string address);
    }
}