using System.Security.Claims;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace InfrastructureApp_Tests
{
    public class HomePageRecentReportsTests
    {
        private FakeReportIssueRepository _fakeRepo = null!;

        [SetUp]
        public void Setup()
        {
            // Setup fake repository before each test
            _fakeRepo = new FakeReportIssueRepository();
        }

        // -------------------------------------------------------
        // TEST 1: Check Index returns a ViewResult
        // This tests that the Home page loads correctly
        // -------------------------------------------------------
        [Test]
        public async Task Index_ReturnsViewResult()
        {
            // Arrange
            var controller = new HomeController(
                NullLogger<HomeController>.Instance,
                _fakeRepo
            );

            // Act
            var result = await controller.Index();

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        // -------------------------------------------------------
        // TEST 2: Check only 3 reports are shown
        // Even if more reports exist, only 3 should display
        // -------------------------------------------------------
        [Test]
        public async Task Index_ReturnsOnlyThreeReports_WhenMoreThanThreeExist()
        {
            // Arrange
            _fakeRepo.LatestReports = new List<ReportIssue>
            {
                new ReportIssue { Id = 1, Description = "Report 1", Status = "Approved" },
                new ReportIssue { Id = 2, Description = "Report 2", Status = "Approved" },
                new ReportIssue { Id = 3, Description = "Report 3", Status = "Approved" },
                new ReportIssue { Id = 4, Description = "Report 4", Status = "Approved" }
            };

            var controller = new HomeController(
                NullLogger<HomeController>.Instance,
                _fakeRepo
            );

            // Act
            var result = await controller.Index() as ViewResult;
            var model = result?.Model as List<ReportIssue>;

            // Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Count, Is.EqualTo(3));
        }

        // -------------------------------------------------------
        // TEST 3: Check empty list when no reports exist
        // Home page should show empty list instead of crashing
        // -------------------------------------------------------
        [Test]
        public async Task Index_ReturnsEmptyList_WhenRepositoryReturnsNoReports()
        {
            // Arrange
            _fakeRepo.LatestReports = new List<ReportIssue>();

            var controller = new HomeController(
                NullLogger<HomeController>.Instance,
                _fakeRepo
            );

            // Act
            var result = await controller.Index() as ViewResult;
            var model = result?.Model as List<ReportIssue>;

            // Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------
        // TEST 4: Check admin user passes true to repository
        // Admin should see all reports
        // -------------------------------------------------------
        [Test]
        public async Task Index_PassesAdminTrue_WhenUserIsAdmin()
        {
            // Arrange
            var adminUser = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Role, "Admin") },
                    "TestAuth"));

            var controller = new HomeController(
                NullLogger<HomeController>.Instance,
                _fakeRepo
            );

            // Set fake logged-in admin user
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = adminUser
                }
            };

            // Act
            await controller.Index();

            // Assert
            Assert.That(_fakeRepo.LastIsAdminValue, Is.True);
        }

        // -------------------------------------------------------
        // TEST 5: Check behavior when repository is missing
        // Should return empty list instead of crashing
        // -------------------------------------------------------
        [Test]
        public async Task Index_ReturnsEmptyList_WhenRepositoryIsNotProvided()
        {
            // Arrange
            var controller = new HomeController(
                NullLogger<HomeController>.Instance
            );

            // Act
            var result = await controller.Index() as ViewResult;
            var model = result?.Model as List<ReportIssue>;

            // Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------
        // Fake Repository for testing (no real database)
        // -------------------------------------------------------
        private class FakeReportIssueRepository : IReportIssueRepository
        {
            public List<ReportIssue> LatestReports { get; set; } = new();
            public bool LastIsAdminValue { get; private set; }

            public Task<ReportIssue?> GetByIdAsync(int id)
            {
                return Task.FromResult<ReportIssue?>(null);
            }

            public Task AddAsync(ReportIssue report)
            {
                return Task.CompletedTask;
            }

            public Task SaveChangesAsync()
            {
                return Task.CompletedTask;
            }

            public Task<List<ReportIssue>> GetLatestReportsAsync(bool isAdmin)
            {
                LastIsAdminValue = isAdmin;
                return Task.FromResult(LatestReports);
            }

            public Task<List<ReportIssue>> SearchLatestReportsAsync(bool isAdmin, string? keyword, string? sort)
            {
                return Task.FromResult(new List<ReportIssue>());
            }
        }
    }
}