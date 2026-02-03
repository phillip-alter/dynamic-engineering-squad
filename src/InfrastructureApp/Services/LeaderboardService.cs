using InfrastructureApp.Models;

namespace InfrastructureApp.Services;

public class LeaderboardService
{
    private readonly ILeaderboardRepository _repo;

    public LeaderboardService(ILeaderboardRepository repo)
    {
        _repo = repo;
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(int n = 25)
    {
        if (n <= 0) n = 25;

        var all = await _repo.GetAllAsync();

        //Sort rules:
        //1. points desc
        //2. display name asc
        //3. updatedAt desc
        var ordered = all
            .OrderByDescending(e => e.ContributionPoints)
            .ThenBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(e => e.UpdatedAtUtc)
            .Take(n)
            .ToList()
            .AsReadOnly();
        return ordered;    
    }

    public async Task AddPointsAsync(string displayName, int pointsToAdd)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("DisplayName is required.", nameof(displayName));

        var name = displayName.Trim();
        if (name.Length > 64)
            throw new ArgumentException("DisplayName must be 64 characters or fewer", nameof(displayName));

        //Points cannot be negative: enforced by requiring pointsToAdd > 0
        await _repo.UpsertAddPointsAsync(name, pointsToAdd, DateTime.UtcNow);

    }

    public Task SeedIfEmptyAsync(IEnumerable<LeaderboardEntry> seedEntries)
        => _repo.SeedIfEmptyAsync(seedEntries);
}