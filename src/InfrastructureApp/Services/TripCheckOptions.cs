namespace InfrastructureApp.Services
{
    public class TripCheckOptions
    {
        //how long we keep cameras cached
        public int CacheMinutes { get; set; } = 10;
    }
}