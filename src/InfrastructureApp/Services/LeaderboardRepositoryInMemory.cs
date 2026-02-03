using System.Collections.Concurrent;
using InfrastructureApp.Models;

namespace InfrastructureApp.Services;

public class LeaderboardRepositoryInMemory : ILeaderboardRepository
{
    //Keyed by DisplayName (case-insensitive)
    ProviderAliasAttribute readonly ConcurrentDictionary<string, LeaderboardEntry> _entries = 
        new(StringComparer.OrdinalIgnoreCase);

        //Lock Protects multi-step updates (read-modify-write)
        private readonly object _lock = new();

        public Task<IReadOnlyCollection<LeaderboardEntry>> GetAllAsync()
    {
        
        //Return copies to prevent external mutation
        var snapshot = _entries.Values  
            .Select(e => new LeaderboardEntry
            {
                DisplayName = e.DisplayName,
                ContributionPoints = e.ContributionPoints,
                UpdatedAtUtc = e.UpdatedAtUtc
            })
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyCollection<LeaderboardEntry>>(snapshot);    
    }


public Task UpsertAddPointAsync(string displayName, int pointsToAdd, DateTime updatedAtUtc)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(displayName, out var existing))
            {
                existing.ContributionPoints += pointsToAdd;
                existing.UpdatedAtUtc = updatedAtUtc;
            }
            else
            {
                _entries[displayName] = new LeaderboardEntry
                {
                    DisplayName = displayName,
                    ContributionPoints = pointsToAdd,
                    UpdatedAtUtc = updatedAtUtc
                };

            }
        }
        return Task.CompletedTask;
    }

    public Task SeedIfEmptyAsync(IEnumerable<LeaderboardEntry> seedEntries)
    {
        lock (_lock)
        {
            if (_entries.Count > 0) return Task.CompletedTask;

            foreach (var e in seedEntries)
            {
                //Avoid seeding bad data
                if (string.IsNullOrWhiteSpace(e.DisplayName)) continue;
                if (e.ContributionPoints < 0) continue;

                _entries[e.DisplayName.Trim()] = new LeaderboardEntry
                {
                    DisplayName = e.DisplayName.Trim(),
                    ContributionPoints = e.ContributionPoints,
                    UpdatedAtUtc = e.UpdatedAtUtc == default ? DateTime.UtcNow : e.UpdatedAtUtc
                };
            }
        }
        return Task.CompletedTask;
    }
}