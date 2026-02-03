using InfrastructureApp.Models;

namespace InfrastructureApp.ViewModels;

public class LeaderboardIndexViewModel
{
    public IReadOnlyList<LeaderboardEntry> Entries { get; init; } = Array.Empty<LeaderboardEntry>();

    
    public AddPointsRequest Form { get; init; } = new();

    public string? ErrorMessage { get; init; }
    public int TopN { get; init; } = 25;
}
