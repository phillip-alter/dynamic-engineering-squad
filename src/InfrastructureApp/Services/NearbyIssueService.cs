using InfrastructureApp.Data;
using InfrastructureApp.Dtos;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class NearbyIssueService : INearbyIssueService
    {
        private readonly ApplicationDbContext _db;
        
        public NearbyIssueService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<NearbyIssueDTO>> GetNearbyIssuesAsync(double lat, double lng, double radiusMiles)
        {
            if (radiusMiles <= 0 || radiusMiles > 100) radiusMiles = 5;

            var candidates = await _db.ReportIssue
                .AsNoTracking()
                .Where(r => r.Latitude != null && r.Longitude != null)
                .Select(r => new
                {
                    r.Id,
                    r.Status,
                    r.CreatedAt,
                    Lat = (double)r.Latitude!.Value,
                    Lng = (double)r.Longitude!.Value
                })
                .ToListAsync();

            // Compute distance in memory (simple + DB-agnostic)
            var filtered = candidates
                .Select(r => new
                {
                    r.Id,
                    r.Status,
                    r.CreatedAt,
                    r.Lat,
                    r.Lng,
                    Distance = HaversineMiles(lat, lng, r.Lat, r.Lng)
                })
                .Where(r => r.Distance <= radiusMiles)
                .OrderBy(r => r.Distance)
                .Take(300)
                .ToList();

            // We can’t build MVC Url.Action here without a request context.
            // So: return relative route pattern (or inject a URL builder with access to HttpContext).
            // The controller can fill DetailsUrl if you want fully correct routes.

            return filtered.Select(r => new NearbyIssueDTO
            {
                Id = r.Id,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                Latitude = r.Lat,
                Longitude = r.Lng,
                DistanceMiles = r.Distance,
            }).ToList();
        }

        private static double HaversineMiles(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 3958.756;

            static double ToRad(double d) => (Math.PI / 180.0) * d;

            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);

            var a =
                Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Pow(Math.Sin(dLon / 2), 2);

            var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            return R * c;
        }
    }
}