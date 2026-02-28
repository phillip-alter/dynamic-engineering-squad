using System;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;     // ReportIssue entity (EF Core table model)
using InfrastructureApp.Services;   // NearbyIssueService (class under test)
using Microsoft.Data.Sqlite;        // SQLite in-memory connection
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using Microsoft.AspNetCore.Http;


namespace InfrastructureApp_Tests.Services
{
    [TestFixture]
    public class NearbyIssueServiceTests
    {
        private LinkGenerator _links = null!;

        [SetUp]
        public void SetUp()
        {
            _links = Substitute.For<LinkGenerator>();
        }

        // ----------------------------
        // Test helper: Build a SQLite in-memory EF Core database
        // ----------------------------
        // Key difference vs EF InMemory provider:
        // - SQLite is a real relational database engine
        // - The in-memory database exists ONLY as long as the connection is open
        //
        // So this method returns BOTH:
        // - the DbContext, and
        // - the open SqliteConnection that must stay alive for the test.
        private static ApplicationDbContext BuildDb(out SqliteConnection conn)
        {
            conn = new SqliteConnection("Filename=:memory:");
            conn.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(conn)
                .Options;

            var db = new ApplicationDbContext(options);

            // Create tables based on your EF Core model
            // (without this, SQLite will throw "no such table: ReportIssue")
            db.Database.EnsureCreated();

            return db;
        }

        // ----------------------------
        // Test helper: Create a ReportIssue with common defaults
        // ----------------------------
        private static ReportIssue Issue(
            int id,
            decimal? lat,
            decimal? lng,
            string status = "Approved",
            DateTime? createdAt = null)
        {
            return new ReportIssue
            {
                Id = id,
                Status = status,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                Latitude = lat,
                Longitude = lng
            };
        }

        [Test]
        public async Task GetNearbyIssuesAsync_FiltersOutReportsWithNullCoordinates()
        {
            // Arrange: create db + keep connection open for the life of the test
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);

            db.ReportIssue.AddRange(
                Issue(1, null, -123.23m),      // missing lat => excluded by .Where(...)
                Issue(2, 44.84m, null),        // missing lng => excluded by .Where(...)
                Issue(3, 44.84m, -123.23m)     // valid => included
            );
            await db.SaveChangesAsync();

            // Act
            var results = await service.GetNearbyIssuesAsync(44.84, -123.23, 5);

            // Assert
            Assert.That(results.Select(r => r.Id), Is.EquivalentTo(new[] { 3 }));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_RadiusDefaultsTo5_WhenRadiusIsInvalid()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);

            // One issue at query point (distance ~ 0) so it will always pass radius=5
            db.ReportIssue.Add(Issue(1, 44.84m, -123.23m));
            await db.SaveChangesAsync();

            // Act
            var resultsInvalidLow = await service.GetNearbyIssuesAsync(44.84, -123.23, 0);
            var resultsInvalidHigh = await service.GetNearbyIssuesAsync(44.84, -123.23, 500);
            var resultsDefault = await service.GetNearbyIssuesAsync(44.84, -123.23, 5);

            // Assert: invalid radii behave the same as radius=5
            Assert.That(resultsInvalidLow.Select(x => x.Id), Is.EquivalentTo(resultsDefault.Select(x => x.Id)));
            Assert.That(resultsInvalidHigh.Select(x => x.Id), Is.EquivalentTo(resultsDefault.Select(x => x.Id)));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_ComputesDistanceMiles_AndIncludesOnlyWithinRadius()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);

            const double qLat = 44.84;
            const double qLng = -123.23;

            // At center => distance ~ 0
            db.ReportIssue.Add(Issue(1, (decimal)qLat, (decimal)qLng));

            // ~0.20 degrees lat away ≈ ~13.8 miles
            db.ReportIssue.Add(Issue(2, 45.04m, -123.23m));

            await db.SaveChangesAsync();

            // radius 1 mile => only issue 1
            var within1Mile = await service.GetNearbyIssuesAsync(qLat, qLng, 1);
            Assert.That(within1Mile.Select(r => r.Id), Is.EquivalentTo(new[] { 1 }));
            Assert.That(within1Mile.Single().DistanceMiles, Is.Not.Null);
            Assert.That(within1Mile.Single().DistanceMiles!.Value, Is.LessThan(0.05));

            // radius 20 miles => both
            var within20Miles = await service.GetNearbyIssuesAsync(qLat, qLng, 20);
            Assert.That(within20Miles.Select(r => r.Id), Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(within20Miles.First(r => r.Id == 2).DistanceMiles, Is.GreaterThan(10.0));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_SortsResultsByDistanceAscending()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);

            const double qLat = 44.84;
            const double qLng = -123.23;

            db.ReportIssue.AddRange(
                Issue(1, 44.841m, -123.23m), // closest
                Issue(2, 44.850m, -123.23m), // farther
                Issue(3, 44.900m, -123.23m)  // farthest
            );
            await db.SaveChangesAsync();

            var results = await service.GetNearbyIssuesAsync(qLat, qLng, 50);

            var distances = results.Select(r => r.DistanceMiles ?? double.MaxValue).ToList();
            Assert.That(distances, Is.EqualTo(distances.OrderBy(d => d).ToList()));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_ReturnsAtMost300Results()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);

            // 350 issues at query point => all within radius => output should be capped by Take(300)
            for (int i = 1; i <= 350; i++)
            {
                db.ReportIssue.Add(Issue(i, 44.84m, -123.23m));
            }
            await db.SaveChangesAsync();

            var results = await service.GetNearbyIssuesAsync(44.84, -123.23, 5);

            Assert.That(results.Count, Is.EqualTo(300));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_MapsFieldsCorrectlyIntoDto()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);

            var created = new DateTime(2026, 2, 24, 12, 0, 0, DateTimeKind.Utc);

            db.ReportIssue.Add(Issue(
                id: 99,
                lat: 44.84m,
                lng: -123.23m,
                status: "Approved",
                createdAt: created
            ));
            await db.SaveChangesAsync();

            var results = await service.GetNearbyIssuesAsync(44.84, -123.23, 5);
            var dto = results.Single();

            Assert.That(dto.Id, Is.EqualTo(99));
            Assert.That(dto.Status, Is.EqualTo("Approved"));
            Assert.That(dto.CreatedAt, Is.EqualTo(created));
            Assert.That(dto.Latitude, Is.EqualTo(44.84).Within(0.000001));
            Assert.That(dto.Longitude, Is.EqualTo(-123.23).Within(0.000001));
            Assert.That(dto.DistanceMiles, Is.Not.Null);
            Assert.That(dto.DetailsUrl, Is.EqualTo("/ReportIssue/Details/99"));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_DoesNotTrackEntities_FromQuery_AsNoTrackingIntended()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);

            db.ReportIssue.Add(Issue(1, 44.84m, -123.23m));
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();

            _ = await service.GetNearbyIssuesAsync(44.84, -123.23, 5);

            Assert.That(db.ChangeTracker.Entries().Count(), Is.EqualTo(0));
        }
    }
}