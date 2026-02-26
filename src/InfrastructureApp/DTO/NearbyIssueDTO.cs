//test DTOs indirectly through Service tests.

// A nearby infrastructure issue returned to the map UI
// Namespace that holds objects used ONLY for transferring data
// between layers (Service → API → Frontend)
// DTO = Data Transfer Object
    //
    // Purpose:
    // Represents a nearby infrastructure issue returned by
    // the "nearby issues" query.
    //
    // IMPORTANT:
    // This is NOT a database entity.
    // It is a SAFE object used for API responses.
namespace InfrastructureApp.Dtos
{
    public class NearbyIssueDTO
    {
        // Unique identifier of the report
        // Used for links, map markers, and detail pages
        public int Id { get; init; }

        // Current workflow status of the issue
        // Examples: "Pending", "Approved", "Resolved"
        public string Status { get; init; } = "";
        public DateTime CreatedAt { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }

        // Distance from the user's current location
        // Calculated by service layer (NOT stored in database)
        public double? DistanceMiles { get; init; }
        
    }
}