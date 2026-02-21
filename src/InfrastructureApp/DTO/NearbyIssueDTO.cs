namespace InfrastructureApp.Dtos
{
    public class NearbyIssueDTO
    {
        public int Id { get; init; }
        public string Status { get; init; } = "";
        public DateTime CreatedAt { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public double? DistanceMiles { get; init; }
        
    }
}