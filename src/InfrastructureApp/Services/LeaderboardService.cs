using InfrastructureApp.Models;

namespace InfrastructureApp.Services;

//LeaderboardService contains the business logic for the leaderboard operations.
//It will
//-enforce domain rules
//-coordinate with the repository for data access
//-Shield controllers from persistence and ordering details.


//Keeping this logic here (instead of controllers or repos)
//results in a clean separation of concerns and testable behavior.

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
        //1. Userpoints desc
        //2. UserId asc
        //3. updatedAt desc
        var ordered = all
            .OrderByDescending(e => e.UserPoints)
            .ThenBy(e => e.UserId)
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

        if (pointsToAdd <=0)
            throw new ArgumentException("Points must be greater than 0.", nameof(pointsToAdd));    

        //Points cannot be negative: enforced by requiring pointsToAdd > 0
        await _repo.UpsertAddPointsAsync(name, pointsToAdd, DateTime.UtcNow);

    }

    public Task SeedIfEmptyAsync(IEnumerable<LeaderboardEntry> seedEntries)
        => _repo.SeedIfEmptyAsync(seedEntries);
}