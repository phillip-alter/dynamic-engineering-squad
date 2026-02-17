namespace InfrastructureApp.Services
{
    public class TripCheckOptions
    {
        public int CacheMinutes { get; set; } = 10;

        public string? SubscriptionKey { get; set; }

        public string BaseUrl { get; set; } = "https://api.odot.state.or.us/tripcheck/";
    }
}

