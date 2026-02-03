

namespace InfrastructureApp.Models;

public class LeaderboardEntry
{
    public string DisplayName { get; set; } = "";
    public int ContributionPoints { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}