using InfrastructureApp.Models;

namespace InfrastructureApp.Services;

public interface ILeaderboardRepository
{
    Task<IReadOnlyCollection<LeaderboardEntry>> GetAllAsync();
}
