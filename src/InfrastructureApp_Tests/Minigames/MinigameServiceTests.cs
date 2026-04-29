using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services.Minigames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InfrastructureApp_Tests.Minigames
{
    [TestFixture]
    public class MinigameServiceTests
    {
        private ApplicationDbContext _db = null!;
        private MinigameService _service = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("MinigameServiceTest_" + Guid.NewGuid())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _db = new ApplicationDbContext(options);
            _service = new MinigameService(_db);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        [Test]
        public async Task CompleteGameAsync_MatchingAwardsOnePointPerBoardClear()
        {
            var result = await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));

            Assert.That(result.AwardedPoints, Is.EqualTo(1));
            Assert.That(result.DailyPointsEarned, Is.EqualTo(1));
            Assert.That(result.HasReachedDailyLimit, Is.False);
        }

        [Test]
        public async Task CompleteGameAsync_MatchingCanBeCompletedMultipleTimesUntilDailyCap()
        {
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            var first = await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, date);
            var second = await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, date.AddHours(2));

            Assert.That(first.AwardedPoints, Is.EqualTo(1));
            Assert.That(second.AwardedPoints, Is.EqualTo(1));
            Assert.That(second.DailyPointsEarned, Is.EqualTo(2));
            Assert.That(second.HasReachedDailyLimit, Is.False);

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(2));
            Assert.That(points.LifetimePoints, Is.EqualTo(2));
        }

        [Test]
        public async Task CompleteGameAsync_CompletingDifferentGamesOnSameDayAwardsEachGame()
        {
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, date);
            await _service.CompleteGameAsync("user-1", MinigameConstants.TriviaGameKey, date);

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(6));
            Assert.That(points.LifetimePoints, Is.EqualTo(6));
            Assert.That(await _db.MinigamePlays.CountAsync(), Is.EqualTo(2));
        }

        [Test]
        public async Task CompleteGameAsync_CompletingSameGameOnDifferentDayAwardsAgain()
        {
            await _service.CompleteGameAsync("user-1", MinigameConstants.TriviaGameKey, new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));
            var second = await _service.CompleteGameAsync("user-1", MinigameConstants.TriviaGameKey, new DateTime(2026, 4, 29, 10, 0, 0, DateTimeKind.Utc));

            Assert.That(second.AwardedPoints, Is.EqualTo(5));

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(10));
            Assert.That(points.LifetimePoints, Is.EqualTo(10));
        }

        [Test]
        public void CompleteGameAsync_InvalidGameKeyIsRejected()
        {
            Assert.That(
                async () => await _service.CompleteGameAsync("user-1", "invalid-game", new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc)),
                Throws.ArgumentException);
        }

        [Test]
        public async Task CompleteGameAsync_PointsAreAddedToCurrentAndLifetimeTotals()
        {
            await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(1));
            Assert.That(points.LifetimePoints, Is.EqualTo(1));
        }

        [Test]
        public async Task CompleteGameAsync_MatchingStopsAwardingAfterFiveBoardClearsInOneDay()
        {
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            MinigameAwardResult result = null!;
            for (var i = 0; i < 6; i++)
            {
                result = await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, date.AddMinutes(i));
            }

            Assert.That(result.AwardedPoints, Is.EqualTo(0));
            Assert.That(result.DailyPointsEarned, Is.EqualTo(5));
            Assert.That(result.HasReachedDailyLimit, Is.True);

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(5));
            Assert.That(points.LifetimePoints, Is.EqualTo(5));
        }

        [Test]
        public async Task CompleteGameAsync_DuplicateProtectionWhenCalledTwiceQuicklyCreatesOnePlayRecord()
        {
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            await _service.CompleteGameAsync("user-1", MinigameConstants.TapRepairGameKey, date);
            await _service.CompleteGameAsync("user-1", MinigameConstants.TapRepairGameKey, date);

            Assert.That(await _db.MinigamePlays.CountAsync(), Is.EqualTo(1));
            Assert.That(await _db.UserPoints.Select(x => x.CurrentPoints).SingleAsync(), Is.EqualTo(5));
        }

        [Test]
        public async Task SpinSlotsAsync_NonWinningSpinAwardsNoPoints()
        {
            var service = new MinigameService(_db, () => new[] { "cone", "bridge", "pothole" });
            var result = await service.SpinSlotsAsync("user-1", new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));

            Assert.That(result.Symbols.Count, Is.EqualTo(3));
            Assert.That(result.Symbols.All(symbol => MinigameConstants.SlotSymbols.Contains(symbol)), Is.True);
            Assert.That(result.IsWinningSpin, Is.False);
            Assert.That(result.AwardedPoints, Is.EqualTo(0));
            Assert.That(await _db.UserPoints.AnyAsync(), Is.False);
        }

        [Test]
        public async Task SpinSlotsAsync_WinningSpinAwardsOnePoint()
        {
            var service = new MinigameService(_db, () => new[] { "cone", "cone", "cone" });
            var result = await service.SpinSlotsAsync("user-1", new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));

            Assert.That(result.IsWinningSpin, Is.True);
            Assert.That(result.AwardedPoints, Is.EqualTo(1));
            Assert.That(result.DailyPointsEarned, Is.EqualTo(1));

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(1));
            Assert.That(points.LifetimePoints, Is.EqualTo(1));
        }

        [Test]
        public async Task SpinSlotsAsync_WinningSpinsCapAtFivePointsPerDay()
        {
            var service = new MinigameService(_db, () => new[] { "bridge", "bridge", "bridge" });
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            SlotsSpinResult result = null!;
            for (var i = 0; i < 6; i++)
            {
                result = await service.SpinSlotsAsync("user-1", date.AddMinutes(i));
            }

            Assert.That(result.DailyPointsEarned, Is.EqualTo(5));
            Assert.That(result.HasReachedDailyLimit, Is.True);
            Assert.That(result.AwardedPoints, Is.EqualTo(0));

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(5));
            Assert.That(points.LifetimePoints, Is.EqualTo(5));
        }

        [Test]
        public async Task SpinSlotsAsync_WinningSpinAfterDailyCapAwardsNoAdditionalPoints()
        {
            var service = new MinigameService(_db, () => new[] { "traffic-light", "traffic-light", "traffic-light" });
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < 5; i++)
            {
                await service.SpinSlotsAsync("user-1", date.AddMinutes(i));
            }

            var cappedResult = await service.SpinSlotsAsync("user-1", date.AddMinutes(6));

            Assert.That(cappedResult.AwardedPoints, Is.EqualTo(0));
            Assert.That(cappedResult.DailyPointsEarned, Is.EqualTo(5));
            Assert.That(cappedResult.HasReachedDailyLimit, Is.True);
        }
    }
}
