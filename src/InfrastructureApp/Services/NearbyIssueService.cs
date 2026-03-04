//pulls all reports that have coordinates
//computes distances using the Haversine formula
//filters/sorts them
//returns a list of NearbyIssueDTO objects for your API to send to the map UI




using InfrastructureApp.Data;
using InfrastructureApp.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;

namespace InfrastructureApp.Services
{
    public class NearbyIssueService : INearbyIssueService
    {
        private readonly ApplicationDbContext _db;
        private readonly LinkGenerator _links;    //generates URLs based on apps routing configuration
        
        public NearbyIssueService(ApplicationDbContext db, LinkGenerator links)
        {
            _db = db;
            _links = links;
        }

        // Returns reports within radiusMiles of (lat, lng)
        public async Task<List<NearbyIssueDTO>> GetNearbyIssuesAsync(double lat, double lng, double radiusMiles)
        {
            // default to 5 miles to protect performance.
            if (radiusMiles <= 0 || radiusMiles > 100) radiusMiles = 5;

            // 1) Query DB for all reports that have coordinates.
            // AsNoTracking() improves performance because we only read data,
            // and don't plan to update these entities.
            // NOTE: The Select(...) projects only the fields we need
            // (instead of pulling entire ReportIssue entities).
            var candidates = await _db.ReportIssue
                .AsNoTracking()
                .Where(r => r.Latitude != null && r.Longitude != null)
                .Select(r => new
                {
                    r.Id,
                    r.Status,
                    r.CreatedAt,

                    // Convert nullable decimals into doubles for distance math
                    Lat = (double)r.Latitude!.Value,
                    Lng = (double)r.Longitude!.Value
                })
                .ToListAsync();

            // 2) Compute distance for each candidate IN MEMORY.
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


            // 3) Convert into DTO objects (API-safe payload)
            // DTOs protect you from exposing EF entities directly.
            return filtered.Select(r =>
            {
                var path = _links.GetPathByAction(
                    action: "Details",
                    controller: "ReportIssue",
                    values: new { id = r.Id }
                );

                return new NearbyIssueDTO
                {
                    Id = r.Id,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    Latitude = r.Lat,
                    Longitude = r.Lng,
                    DistanceMiles = r.Distance,
                    DetailsUrl = string.IsNullOrWhiteSpace(path)
                        ? $"/ReportIssue/Details/{r.Id}"
                        : path
                };
            }).ToList();
        }

        //Uses Haversine formula to calculate the shortest distance between two points on a sphere using their latitude and longitude
        private static double HaversineMiles(double lat1, double lon1, double lat2, double lon2)
        {
            // Earth's radius in miles (approx)
            const double R = 3958.756;

            //convert degrees to radians (trig functions use radians)
            static double ToRad(double d) => (Math.PI / 180.0) * d;

            // Differences in radians
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);

            // Haversine formula (great-circle distance)
            var a =
                Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Pow(Math.Sin(dLon / 2), 2);

            // c is the angular distance in radians
            var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));

            // Convert angular distance to miles
            return R * c;
        }
    }
}