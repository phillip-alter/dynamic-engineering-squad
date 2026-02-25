/* test for business logic that sits between your controller and EF Core */

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class ReportIssueServiceTests
    {
        private SqliteConnection _conn = null!;
        private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
        private string _webRoot = null!;

        [SetUp]
        public void SetUp()
        {
            // SQLite in-memory: keep connection open for lifetime of the test
            _conn = new SqliteConnection("Filename=:memory:");
            _conn.Open();

            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_conn)
                .Options;

            // temp web root for file save tests
            _webRoot = Path.Combine(Path.GetTempPath(), "InfraAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_webRoot);

            // create schema
            using var db = new ApplicationDbContext(_dbOptions);
            db.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_webRoot))
                    Directory.Delete(_webRoot, recursive: true);
            }
            catch { /* ignore cleanup failures */ }

            _conn?.Dispose();
        }

        private ApplicationDbContext NewDb() => new ApplicationDbContext(_dbOptions);

        private static IFormFile MakeFormFile(byte[] bytes, string fileName, string contentType = "image/png")
        {
            var ms = new MemoryStream(bytes);
            return new FormFile(ms, 0, bytes.Length, "Photo", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private ReportIssueService MakeService(ApplicationDbContext db)
        {
            var env = Substitute.For<IWebHostEnvironment>();
            env.WebRootPath.Returns(_webRoot);

            IReportIssueRepository repo = new TestReportIssueRepository(db);

            return new ReportIssueService(db, repo, env);
        }

        // -------
        // Tests
        // -------

        //creates user report and assigns points
        [Test]
        public async Task CreateAsync_NoExistingUserPoints_CreatesReport_Adds10Points()
        {
            using var db = NewDb();
            var service = MakeService(db);

            var vm = new ReportIssueViewModel
            {
                Description = "Pothole on Main",
                Latitude = 44.85m,
                Longitude = -123.23m,
                Photo = null
            };

            var userId = "user-1";

            var reportId = await service.CreateAsync(vm, userId);

            // verify report saved (DbSet name is ReportIssue)
            var report = await db.ReportIssue.FirstOrDefaultAsync(r => r.Id == reportId);
            Assert.That(report, Is.Not.Null);
            Assert.That(report!.UserId, Is.EqualTo(userId));
            Assert.That(report.Description, Is.EqualTo("Pothole on Main"));
            Assert.That(report.Status, Is.EqualTo("Approved"));
            Assert.That(report.ImageUrl, Is.Null);

            // verify points created + updated
            var points = await db.UserPoints.FirstOrDefaultAsync(p => p.UserId == userId);
            Assert.That(points, Is.Not.Null);
            Assert.That(points!.CurrentPoints, Is.EqualTo(10));
            Assert.That(points.LifetimePoints, Is.EqualTo(10));
        }

        //if user already has points, it should add +10 points
        [Test]
        public async Task CreateAsync_ExistingUserPoints_IncrementsBy10()
        {
            using var db = NewDb();

            db.UserPoints.Add(new UserPoints
            {
                UserId = "user-2",
                CurrentPoints = 5,
                LifetimePoints = 50,
                LastUpdated = DateTime.UtcNow.AddDays(-1)
            });
            await db.SaveChangesAsync();

            var service = MakeService(db);

            var vm = new ReportIssueViewModel
            {
                Description = "Streetlight out",
                Photo = null
            };

            var reportId = await service.CreateAsync(vm, "user-2");

            Assert.That(reportId, Is.GreaterThan(0));

            var points = await db.UserPoints.SingleAsync(p => p.UserId == "user-2");
            Assert.That(points.CurrentPoints, Is.EqualTo(15));
            Assert.That(points.LifetimePoints, Is.EqualTo(60));
        }

        //when valid image is provided, file is saved in wwwroot/uploads/issues and imageurl is set
        [Test]
        public async Task CreateAsync_WithValidPhoto_SavesFile_SetsImageUrl()
        {
            using var db = NewDb();
            var service = MakeService(db);

            var bytes = new byte[] { 1, 2, 3, 4, 5 }; // tiny
            var vm = new ReportIssueViewModel
            {
                Description = "Cracked sidewalk",
                Photo = MakeFormFile(bytes, "sidewalk.png", "image/png")
            };

            var reportId = await service.CreateAsync(vm, "user-3");

            var report = await db.ReportIssue.SingleAsync(r => r.Id == reportId);

            Assert.That(report.ImageUrl, Is.Not.Null);
            Assert.That(report.ImageUrl, Does.StartWith("/uploads/issues/"));
            Assert.That(report.ImageUrl, Does.EndWith(".png"));

            // verify file exists in webroot/uploads/issues
            var uploadsDir = Path.Combine(_webRoot, "uploads", "issues");
            Assert.That(Directory.Exists(uploadsDir), Is.True);

            var files = Directory.GetFiles(uploadsDir);
            Assert.That(files.Length, Is.EqualTo(1));
            Assert.That(Path.GetExtension(files[0]).ToLowerInvariant(), Is.EqualTo(".png"));
        }

        //Reject unsupported formats (like GIF) and ensure it’s all-or-nothing (no partial saves).
        [Test]
        public void CreateAsync_InvalidExtension_Throws_AndDoesNotCreatePointsOrReport()
        {
            using var db = NewDb();
            var service = MakeService(db);

            var vm = new ReportIssueViewModel
            {
                Description = "Bad file",
                Photo = MakeFormFile(new byte[] { 1, 2, 3 }, "evil.gif", "image/gif")
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.CreateAsync(vm, "user-4"));

            Assert.That(ex!.Message, Does.Contain("Only JPG, PNG, or WEBP"));

            // Should not have created points or reports
            Assert.That(db.UserPoints.Any(p => p.UserId == "user-4"), Is.False);
            Assert.That(db.ReportIssue.Any(r => r.UserId == "user-4"), Is.False);
        }

        //Reject large files (> 5MB) and again ensure no partial saves.
        [Test]
        public void CreateAsync_Over5MB_Throws_AndDoesNotCreatePointsOrReport()
        {
            using var db = NewDb();
            var service = MakeService(db);

            var big = new byte[5 * 1024 * 1024 + 1]; // 5MB + 1
            var vm = new ReportIssueViewModel
            {
                Description = "Too big",
                Photo = MakeFormFile(big, "big.jpg", "image/jpeg")
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.CreateAsync(vm, "user-5"));

            Assert.That(ex!.Message, Does.Contain("5MB or smaller"));

            Assert.That(db.ReportIssue.Any(r => r.UserId == "user-5"), Is.False);
            Assert.That(db.UserPoints.Any(p => p.UserId == "user-5"), Is.False);
        }

        // ----------------
        // Test repository 
        // ----------------

        /* This is a lightweight repository that satisfies IReportIssueRepository interface, but directly uses EF Core:
        AddAsync adds to db.ReportIssue
        GetByIdAsync queries db.ReportIssue
        SaveChangesAsync calls db.SaveChangesAsync()
        It’s basically there so the service can be constructed without needing your real repository implementation (or to keep the tests focused on service logic). */
        private class TestReportIssueRepository : IReportIssueRepository
        {
            private readonly ApplicationDbContext _db;

            public TestReportIssueRepository(ApplicationDbContext db) => _db = db;

            public Task<ReportIssue?> GetByIdAsync(int id)
                => _db.ReportIssue.FirstOrDefaultAsync(r => r.Id == id);

            public Task AddAsync(ReportIssue report)
                => _db.ReportIssue.AddAsync(report).AsTask();

            public Task SaveChangesAsync()
                => _db.SaveChangesAsync();

            public async Task<List<ReportIssue>> GetLatestReportsAsync(bool isAdmin)
            {
                var query = _db.ReportIssue.AsQueryable();

                if (!isAdmin)
                    query = query.Where(r => r.Status == "Approved");

                return await query
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
        }
    }
}
