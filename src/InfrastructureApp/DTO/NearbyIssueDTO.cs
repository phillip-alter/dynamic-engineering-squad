namespace InfrastructureApp.Dtos
{
    public class NearbyReportDto
    {
        public int Id { get; init; }
        public string Status { get; init; } = "";
        public DateTime CreatedAt { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public double? DistanceMiles { get; init; }
        public string DetailsUrl { get; init; } = "";
    }
}