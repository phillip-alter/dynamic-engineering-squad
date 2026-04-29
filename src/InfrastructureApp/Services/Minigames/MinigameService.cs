using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services.Minigames
{
    public class MinigameService : IMinigameService
    {
        private readonly ApplicationDbContext _db;
        private readonly Func<IReadOnlyList<string>> _slotSymbolProvider;

        public MinigameService(ApplicationDbContext db)
            : this(db, CreateDefaultSlotSymbolProvider())
        {
        }

        public MinigameService(ApplicationDbContext db, Func<IReadOnlyList<string>> slotSymbolProvider)
        {
            _db = db;
            _slotSymbolProvider = slotSymbolProvider;
        }

        public async Task<IReadOnlyList<MinigameStatus>> GetTodayStatusesAsync(string userId, DateTime? utcNow = null)
        {
            var today = GetPlayedOnDate(utcNow);

            var todayPlays = await _db.MinigamePlays
                .Where(play => play.UserId == userId && play.PlayedOnDate == today)
                .ToListAsync();

            return MinigameConstants.SupportedGameKeys
                .Select(gameKey => new MinigameStatus
                {
                    GameKey = gameKey,
                    DailyPointsEarned = todayPlays
                        .Where(play => play.GameKey.Equals(gameKey, StringComparison.OrdinalIgnoreCase))
                        .Select(play => play.PointsAwarded)
                        .FirstOrDefault(),
                    DailyPointsLimit = MinigameConstants.PointsPerGame,
                    HasReachedDailyLimit = todayPlays
                        .Where(play => play.GameKey.Equals(gameKey, StringComparison.OrdinalIgnoreCase))
                        .Select(play => play.PointsAwarded >= MinigameConstants.PointsPerGame)
                        .FirstOrDefault()
                })
                .ToList();
        }

        public async Task<int> GetCurrentPointsAsync(string userId)
        {
            return await _db.UserPoints
                .Where(points => points.UserId == userId)
                .Select(points => (int?)points.CurrentPoints)
                .FirstOrDefaultAsync() ?? 0;
        }

        public Task<MinigameAwardResult> CompleteGameAsync(string userId, string gameKey, DateTime? utcNow = null)
        {
            return CompleteGameInternalAsync(userId, gameKey, utcNow);
        }

        public async Task<SlotsSpinResult> SpinSlotsAsync(string userId, DateTime? utcNow = null)
        {
            var symbols = _slotSymbolProvider().ToArray();
            var isWinningSpin = symbols.Length == 3 && symbols.Distinct(StringComparer.Ordinal).Count() == 1;
            var awardResult = await AwardSlotsSpinAsync(userId, isWinningSpin, utcNow);

            return new SlotsSpinResult
            {
                GameKey = awardResult.GameKey,
                AwardedPoints = awardResult.AwardedPoints,
                CurrentPoints = awardResult.CurrentPoints,
                PlayedOnDate = awardResult.PlayedOnDate,
                DailyPointsEarned = awardResult.DailyPointsEarned,
                DailyPointsLimit = awardResult.DailyPointsLimit,
                HasReachedDailyLimit = awardResult.HasReachedDailyLimit,
                Symbols = symbols,
                IsWinningSpin = isWinningSpin,
                ResultLabel = isWinningSpin ? "Three of a Kind" : "No Match"
            };
        }

        private async Task<MinigameAwardResult> CompleteGameInternalAsync(string userId, string gameKey, DateTime? utcNow)
        {
            if (!MinigameConstants.IsSupportedGameKey(gameKey))
            {
                throw new ArgumentException("Invalid minigame key.", nameof(gameKey));
            }

            var normalizedGameKey = gameKey.Trim().ToLowerInvariant();
            var today = GetPlayedOnDate(utcNow);

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var existingPlay = await _db.MinigamePlays
                    .FirstOrDefaultAsync(play =>
                        play.UserId == userId &&
                        play.GameKey == normalizedGameKey &&
                        play.PlayedOnDate == today);

                if (existingPlay != null)
                {
                    await tx.CommitAsync();
                    return new MinigameAwardResult
                    {
                        GameKey = normalizedGameKey,
                        AwardedPoints = 0,
                        CurrentPoints = await GetCurrentPointsAsync(userId),
                        PlayedOnDate = today,
                        DailyPointsEarned = existingPlay.PointsAwarded,
                        DailyPointsLimit = MinigameConstants.PointsPerGame,
                        HasReachedDailyLimit = true
                    };
                }

                var userPoints = await _db.UserPoints.FirstOrDefaultAsync(points => points.UserId == userId);
                if (userPoints == null)
                {
                    userPoints = new UserPoints
                    {
                        UserId = userId,
                        CurrentPoints = 0,
                        LifetimePoints = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                    _db.UserPoints.Add(userPoints);
                }

                _db.MinigamePlays.Add(new MinigamePlay
                {
                    UserId = userId,
                    GameKey = normalizedGameKey,
                    PlayedOnDate = today,
                    PointsAwarded = MinigameConstants.PointsPerGame,
                    CreatedAt = DateTime.UtcNow
                });

                userPoints.CurrentPoints += MinigameConstants.PointsPerGame;
                userPoints.LifetimePoints += MinigameConstants.PointsPerGame;
                userPoints.LastUpdated = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new MinigameAwardResult
                {
                    GameKey = normalizedGameKey,
                    AwardedPoints = MinigameConstants.PointsPerGame,
                    CurrentPoints = userPoints.CurrentPoints,
                    PlayedOnDate = today,
                    DailyPointsEarned = MinigameConstants.PointsPerGame,
                    DailyPointsLimit = MinigameConstants.PointsPerGame,
                    HasReachedDailyLimit = true
                };
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                _db.ChangeTracker.Clear();

                if (await WasAlreadyPlayedTodayAsync(userId, normalizedGameKey, today))
                {
                    return new MinigameAwardResult
                    {
                        GameKey = normalizedGameKey,
                        AwardedPoints = 0,
                        CurrentPoints = await GetCurrentPointsAsync(userId),
                        PlayedOnDate = today,
                        DailyPointsEarned = MinigameConstants.PointsPerGame,
                        DailyPointsLimit = MinigameConstants.PointsPerGame,
                        HasReachedDailyLimit = true
                    };
                }

                throw;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<bool> WasAlreadyPlayedTodayAsync(string userId, string gameKey, DateTime today)
        {
            return await _db.MinigamePlays.AnyAsync(play =>
                play.UserId == userId &&
                play.GameKey == gameKey &&
                play.PlayedOnDate == today);
        }

        private static DateTime GetPlayedOnDate(DateTime? utcNow)
        {
            var now = utcNow ?? DateTime.UtcNow;
            if (now.Kind == DateTimeKind.Unspecified)
            {
                now = DateTime.SpecifyKind(now, DateTimeKind.Utc);
            }

            var localNow = now.Kind == DateTimeKind.Local
                ? now
                : TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.Local);

            return localNow.Date;
        }

        private async Task<MinigameAwardResult> AwardSlotsSpinAsync(string userId, bool isWinningSpin, DateTime? utcNow)
        {
            var today = GetPlayedOnDate(utcNow);

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var existingPlay = await _db.MinigamePlays
                    .FirstOrDefaultAsync(play =>
                        play.UserId == userId &&
                        play.GameKey == MinigameConstants.SlotsGameKey &&
                        play.PlayedOnDate == today);

                var dailyPointsEarned = existingPlay?.PointsAwarded ?? 0;
                var hasReachedDailyLimit = dailyPointsEarned >= MinigameConstants.PointsPerGame;

                if (!isWinningSpin || hasReachedDailyLimit)
                {
                    await tx.CommitAsync();
                    return new MinigameAwardResult
                    {
                        GameKey = MinigameConstants.SlotsGameKey,
                        AwardedPoints = 0,
                        CurrentPoints = await GetCurrentPointsAsync(userId),
                        PlayedOnDate = today,
                        DailyPointsEarned = dailyPointsEarned,
                        DailyPointsLimit = MinigameConstants.PointsPerGame,
                        HasReachedDailyLimit = hasReachedDailyLimit
                    };
                }

                var userPoints = await _db.UserPoints.FirstOrDefaultAsync(points => points.UserId == userId);
                if (userPoints == null)
                {
                    userPoints = new UserPoints
                    {
                        UserId = userId,
                        CurrentPoints = 0,
                        LifetimePoints = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                    _db.UserPoints.Add(userPoints);
                }

                if (existingPlay == null)
                {
                    existingPlay = new MinigamePlay
                    {
                        UserId = userId,
                        GameKey = MinigameConstants.SlotsGameKey,
                        PlayedOnDate = today,
                        PointsAwarded = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.MinigamePlays.Add(existingPlay);
                }

                existingPlay.PointsAwarded += 1;
                userPoints.CurrentPoints += 1;
                userPoints.LifetimePoints += 1;
                userPoints.LastUpdated = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new MinigameAwardResult
                {
                    GameKey = MinigameConstants.SlotsGameKey,
                    AwardedPoints = 1,
                    CurrentPoints = userPoints.CurrentPoints,
                    PlayedOnDate = today,
                    DailyPointsEarned = existingPlay.PointsAwarded,
                    DailyPointsLimit = MinigameConstants.PointsPerGame,
                    HasReachedDailyLimit = existingPlay.PointsAwarded >= MinigameConstants.PointsPerGame
                };
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                _db.ChangeTracker.Clear();
                throw;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static Func<IReadOnlyList<string>> CreateDefaultSlotSymbolProvider()
        {
            var random = new Random();

            return () => Enumerable.Range(0, 3)
                .Select(_ => MinigameConstants.SlotSymbols[random.Next(MinigameConstants.SlotSymbols.Length)])
                .ToArray();
        }
    }
}
