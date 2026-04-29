using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InfrastructureApp.Services.Minigames
{
    public interface IMinigameService
    {
        Task<IReadOnlyList<MinigameStatus>> GetTodayStatusesAsync(string userId, DateTime? utcNow = null);

        Task<int> GetCurrentPointsAsync(string userId);

        Task<MinigameAwardResult> CompleteGameAsync(string userId, string gameKey, DateTime? utcNow = null);

        Task<SlotsSpinResult> SpinSlotsAsync(string userId, DateTime? utcNow = null);
    }
}
