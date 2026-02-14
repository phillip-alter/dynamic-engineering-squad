using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services;

public class LeaderboardRepositoryEf : ILeaderboardRepository
{
    private readonly ApplicationDbContext _db;

    public LeaderboardRepositoryEf(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<LeaderboardEntry>> GetAllAsync()
    {
        var results = await _db.Users
            .Where(u => u.UserName != null)
            .GroupJoin(
                _db.UserPoints,
                u => u.Id,
                p => p.UserId,
                (u, pts) => new { u, pts }
            )
            .Select(x => new LeaderboardEntry
            {
                UserId = x.u.UserName!,
                UserPoints = x.pts.Select(p => p.CurrentPoints).FirstOrDefault(),
                UpdatedAtUtc = x.pts.Select(p => p.LastUpdated).FirstOrDefault()
            })
            .ToListAsync();

        return results.AsReadOnly();
    }
}
