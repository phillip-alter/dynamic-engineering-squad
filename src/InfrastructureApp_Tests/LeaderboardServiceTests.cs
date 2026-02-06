using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using System.Collections.ObjectModel;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class LeaderboardServiceTests
    {
        private Mock<ILeaderboardRepository> _repoMock = null!;
        private LeaderboardService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<ILeaderboardRepository>();
            _service = new LeaderboardService(_repoMock.Object);
        }

        [Test]
        public async Task GetTopAsync_CallsRepoGetAllAsync_Once()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(Array.Empty<LeaderboardEntry>());

            // Act
            await _service.GetTopAsync(10);

            // Assert
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-25)]
        public async Task GetTopAsync_WhenNIsNonPositive_DefaultsTo25(int n)
        {
            // Arrange: create > 25 entries to prove default limit works
            var entries = Enumerable.Range(1, 40)
                .Select(i => new LeaderboardEntry
                {
                    UserId = $"user{i:000}",
                    UserPoints = i,
                    UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
                })
                .ToList();

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entries);

            // Act
            var result = await _service.GetTopAsync(n);

            // Assert
            Assert.That(result.Count, Is.EqualTo(25));
        }

        [Test]
        public async Task GetTopAsync_ReturnsAtMostNEntries()
        {
            // Arrange
            var entries = Enumerable.Range(1, 100)
                .Select(i => new LeaderboardEntry
                {
                    UserId = $"user{i:000}",
                    UserPoints = i,
                    UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
                })
                .ToList();

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entries);

            // Act
            var result = await _service.GetTopAsync(10);

            // Assert
            Assert.That(result.Count, Is.EqualTo(10));
        }

        [Test]
        public async Task GetTopAsync_SortsByUserPointsDesc()
        {
            // Arrange
            var t = DateTime.UtcNow;
            var entries = new List<LeaderboardEntry>
            {
                new() { UserId = "a", UserPoints = 10, UpdatedAtUtc = t },
                new() { UserId = "b", UserPoints = 50, UpdatedAtUtc = t },
                new() { UserId = "c", UserPoints = 30, UpdatedAtUtc = t }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entries);

            // Act
            var result = await _service.GetTopAsync(25);

            // Assert
            Assert.That(result.Select(x => x.UserPoints).ToArray(),
                Is.EqualTo(new[] { 50, 30, 10 }));
        }

        [Test]
        public async Task GetTopAsync_WhenPointsTie_SortsByUserIdAsc()
        {
            // Arrange
            var t = DateTime.UtcNow;

            var entries = new List<LeaderboardEntry>
            {
                new() { UserId = "zebra", UserPoints = 100, UpdatedAtUtc = t.AddHours(-1) },
                new() { UserId = "alpha", UserPoints = 100, UpdatedAtUtc = t.AddHours(-2) },
                new() { UserId = "mike",  UserPoints = 100, UpdatedAtUtc = t.AddHours(-3) }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entries);

            // Act
            var result = await _service.GetTopAsync(25);

            // Assert
            Assert.That(result.Select(x => x.UserId).ToArray(),
                Is.EqualTo(new[] { "alpha", "mike", "zebra" }));
        }

        [Test]
        public async Task GetTopAsync_WhenPointsAndUserIdTie_SortsByUpdatedAtUtcDesc()
        {
            // Arrange
            var older = DateTime.UtcNow.AddDays(-2);
            var newer = DateTime.UtcNow.AddDays(-1);

            var entries = new List<LeaderboardEntry>
            {
                new() { UserId = "same", UserPoints = 42, UpdatedAtUtc = older },
                new() { UserId = "same", UserPoints = 42, UpdatedAtUtc = newer }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entries);

            // Act
            var result = await _service.GetTopAsync(25);

            // Assert
            Assert.That(result[0].UpdatedAtUtc, Is.EqualTo(newer));
            Assert.That(result[1].UpdatedAtUtc, Is.EqualTo(older));
        }

        [Test]
        public async Task GetTopAsync_AppliesAllSortRules_Deterministically()
        {
            // Arrange
            var t0 = DateTime.UtcNow;

            var entries = new List<LeaderboardEntry>
            {
                new() { UserId = "bob",   UserPoints = 10, UpdatedAtUtc = t0.AddDays(-1) },
                new() { UserId = "alice", UserPoints = 10, UpdatedAtUtc = t0.AddDays(-2) },
                new() { UserId = "alice", UserPoints = 10, UpdatedAtUtc = t0.AddDays(-1) },
                new() { UserId = "carol", UserPoints = 50, UpdatedAtUtc = t0.AddDays(-5) },
                new() { UserId = "carol", UserPoints = 50, UpdatedAtUtc = t0.AddDays(-1) },
                new() { UserId = "dan",   UserPoints = 50, UpdatedAtUtc = t0.AddDays(-3) },
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entries);

            // Act
            var result = await _service.GetTopAsync(25);

            // Assert expected order:
            // Points 50: carol (newer), carol (older), dan
            // Points 10: alice (newer), alice (older), bob
            var expected = new (string userId, int pts, DateTime updated)[]
            {
                ("carol", 50, t0.AddDays(-1)),
                ("carol", 50, t0.AddDays(-5)),
                ("dan",   50, t0.AddDays(-3)),
                ("alice", 10, t0.AddDays(-1)),
                ("alice", 10, t0.AddDays(-2)),
                ("bob",   10, t0.AddDays(-1)),
            };

            Assert.That(result.Count, Is.EqualTo(expected.Length));

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(result[i].UserId, Is.EqualTo(expected[i].userId), $"UserId mismatch at index {i}");
                Assert.That(result[i].UserPoints, Is.EqualTo(expected[i].pts), $"UserPoints mismatch at index {i}");
                Assert.That(result[i].UpdatedAtUtc, Is.EqualTo(expected[i].updated), $"UpdatedAtUtc mismatch at index {i}");
            }
        }

        [Test]
        public async Task GetTopAsync_ReturnsReadOnlyList()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<LeaderboardEntry>
                     {
                         new() { UserId = "a", UserPoints = 1, UpdatedAtUtc = DateTime.UtcNow }
                     });

            // Act
            var result = await _service.GetTopAsync(25);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<LeaderboardEntry>>());


            Assert.That(result, Is.InstanceOf<ReadOnlyCollection<LeaderboardEntry>>());

            /*Assert.Throws<NotSupportedException>(() =>
            {
                ((ICollection<LeaderboardEntry>)result).Add(new LeaderboardEntry());
            });*/
        }

        [Test]
        public async Task GetTopAsync_WhenRepoReturnsEmpty_ReturnsEmptyReadOnlyList()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(Array.Empty<LeaderboardEntry>());

            // Act
            var result = await _service.GetTopAsync(25);

            // Assert
            Assert.That(result.Count, Is.EqualTo(0));
            Assert.That(result, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<LeaderboardEntry>>());

        }
    }
}
