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

    public async Task UpsertAddPointsAsync(string userId, int pointsToAdd, DateTime updatedAtUtc)
    {
        // Here, "userId" is actually a username/display name coming from the UI/service.
        // Find the Identity user first.
        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserName == userId);
        if (user == null) throw new InvalidOperationException("UserName not found.");

        // IMPORTANT: UserPoints.UserId should store AspNetUsers.Id (the FK), not UserName.
        var row = await _db.UserPoints.SingleOrDefaultAsync(p => p.UserId == user.Id);

        if (row == null)
        {
            _db.UserPoints.Add(new UserPoints
            {
                UserId = user.Id, // <-- FIX: store the Identity user Id (FK)
                CurrentPoints = pointsToAdd,
                LifetimePoints = pointsToAdd,
                LastUpdated = updatedAtUtc
            });
        }
        else
        {
            row.CurrentPoints += pointsToAdd;
            row.LifetimePoints += pointsToAdd;
            row.LastUpdated = updatedAtUtc;
        }

        await _db.SaveChangesAsync();
    }


    public Task SeedIfEmptyAsync(IEnumerable<LeaderboardEntry> seedEntries)
        => Task.CompletedTask;
}
