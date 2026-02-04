
namespace InfrastructureApp.Models
{
    public class LeaderboardEntry
    {
        public string UserId { get; set; } = string.Empty;
        public int UserPoints { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
