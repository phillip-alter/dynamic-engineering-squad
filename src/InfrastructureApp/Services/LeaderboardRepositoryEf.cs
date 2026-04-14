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
        var rows = await _db.Users
            .Where(u => u.UserName != null)
            .GroupJoin(
                _db.UserPoints,
                u => u.Id,
                p => p.UserId,
                (u, pts) => new { u, pts }
            )
            .Select(x => new
            {
                x.u.UserName,
                x.u.AvatarUrl,
                x.u.AvatarKey,
                UserPoints = x.pts.Select(p => p.CurrentPoints).FirstOrDefault(),
                UpdatedAtUtc = x.pts.Select(p => p.LastUpdated).FirstOrDefault()
            })
            .ToListAsync();

        var results = rows.Select(x => new LeaderboardEntry
        {
            UserId = x.UserName!,
            UserPoints = x.UserPoints,
            UpdatedAtUtc = x.UpdatedAtUtc,
            AvatarUrl = !string.IsNullOrWhiteSpace(x.AvatarUrl)
                ? x.AvatarUrl
                : AvatarCatalog.ToUrl(x.AvatarKey)
        }).ToList();

        return results.AsReadOnly();
    }
}
