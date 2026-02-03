using InfrastructureApp.Models;

namespace InfrastructureApp.Services;

public interface ILeaderboardRepository
{
    Task<IReadOnlyCollection<LeaderboardEntry>> GetAllAsync();
    Task UpsertAddPointAsync(string displayName, int pointsToAdd, DateTime updateAtUtc);
    Task SeedIfEmptyAsync(IEnumerable<LeaderboardEntry> seedEntries);
}