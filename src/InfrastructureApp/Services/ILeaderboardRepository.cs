using InfrastructureApp.Models;

namespace InfrastructureApp.Services;

public interface ILeaderboardRepository
{
    Task<IReadOnlyCollection<LeaderboardEntry>> GetAllAsync();
    Task UpsertAddPointsAsync(string displayName, int pointsToAdd, DateTime updatedAtUtc);
    Task SeedIfEmptyAsync(IEnumerable<LeaderboardEntry> seedEntries);
}