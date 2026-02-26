using InfrastructureApp.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace InfrastructureApp_Tests
{
    // Tests for the "Latest Reports" feature logic.
    // Sprint 1 - unit test C# logic in models/repositories/view-models (not UI).
    public class ReportsLatestTests
    {

        [SetUp]
        public void Setup()
        {
            // No shared setup 
        }

        // -------------------------------------------------------
        // TEST 1: Visibility rules (Approved-only for non-admin)
        // Uses the model helper: ReportIssue.VisibleToUser(...)
        // -------------------------------------------------------
        [Test]
        public void VisibleToUser_WhenNotAdmin_ReturnsOnlyApproved()
        {
            // Arrange: fake in-memory data
            var reports = new List<ReportIssue>
            {
                new ReportIssue { Status = "Approved" },
                new ReportIssue { Status = "Pending" },
                new ReportIssue { Status = "Rejected" },
                new ReportIssue { Status = "Approved" }
            }.AsQueryable();

            // Act: non-admin should only see Approved
            var result = ReportIssue.VisibleToUser(reports, isAdmin: false).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(r => r.Status == "Approved"), Is.True);
        }

        // -------------------------------------------------------
        // TEST 2: Visibility rules (Admin sees all)
        // -------------------------------------------------------
        [Test]
        public void VisibleToUser_WhenAdmin_ReturnsAll()
        {
            // Arrange
            var reports = new List<ReportIssue>
            {
                new ReportIssue { Status = "Approved" },
                new ReportIssue { Status = "Pending" },
                new ReportIssue { Status = "Rejected" }
            }.AsQueryable();

            // Act: admin sees everything
            var result = ReportIssue.VisibleToUser(reports, isAdmin: true).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
        }

        // -------------------------------------------------------
        // TEST 3: Newest-first sorting logic
        // Reports must be ordered newest-first.
        // Uses the model helper: ReportIssue.OrderLatestFirst(...)
        // This ensures the Latest page shows most recent reports first.
        // -------------------------------------------------------
        [Test]
        public void OrderLatestFirst_OrdersByCreatedAtDescending()
        {
            // Arrange: create two reports with different dates
            var older = new ReportIssue { CreatedAt = new DateTime(2026, 1, 1) };
            var newer = new ReportIssue { CreatedAt = new DateTime(2026, 2, 1) };

            var reports = new List<ReportIssue> { older, newer }.AsQueryable();

            // Act
            var result = ReportIssue.OrderLatestFirst(reports).ToList();

            // Assert: newest item should be first
            Assert.That(result[0].CreatedAt, Is.EqualTo(newer.CreatedAt));
            Assert.That(result[1].CreatedAt, Is.EqualTo(older.CreatedAt));
        }

        // -------------------------------------------------------
        // TEST 4: Model validation - Required Description
        // -------------------------------------------------------
        [Test]
        public void ReportIssue_ShouldFailValidation_WhenDescriptionMissing()
        {
            // Arrange
            var report = new ReportIssue
            {
                Description = "", // [Required]
                Status = "Approved",
                UserId = "user-guid-001"
            };

            var context = new ValidationContext(report);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(
                report,
                context,
                results,
                validateAllProperties: true);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(
                results.Any(r => r.MemberNames.Contains(nameof(ReportIssue.Description))),
                Is.True
            );
        }
        
        // -------------------------------------------------------
        // TEST 5: Model validation - Latitude range
        // -------------------------------------------------------
        [Test]
        public void ReportIssue_ShouldFailValidation_WhenLatitudeOutOfRange()
        {
            // Arrange
            var report = new ReportIssue
            {
                Description = "Test report",
                Status = "Approved",
                UserId = "user-guid-001",
                Latitude = 200 // invalid: must be between -90 and 90
            };

            var context = new ValidationContext(report);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(
                report,
                context,
                results,
                validateAllProperties: true);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(
                results.Any(r => r.MemberNames.Contains(nameof(ReportIssue.Latitude))),
                Is.True
            );
        }

        // -------------------------------------------------------
        // TEST 6: ImageUrl is preserved through filtering + sorting
        // Ensures the report data includes ImageUrl for SCRUM-81 modal display.
        // -------------------------------------------------------
        [Test]
        public void FilteringAndSorting_ShouldPreserveImageUrl_ForApprovedReports()
        {
            // Arrange: create test data with mixed statuses and image URLs
            var reports = new List<ReportIssue>
    {
        new ReportIssue
        {
            Id = 1,
            Status = "Approved",
            CreatedAt = new DateTime(2026, 1, 1),
            ImageUrl = "https://example.com/old.jpg"
        },
        new ReportIssue
        {
            Id = 2,
            Status = "Approved",
            CreatedAt = new DateTime(2026, 2, 1),
            ImageUrl = "https://example.com/new.jpg"
        },
        new ReportIssue
        {
            Id = 3,
            Status = "Pending",
            CreatedAt = new DateTime(2026, 3, 1),
            ImageUrl = "https://example.com/pending.jpg"
        }
    }.AsQueryable();

            // Act: simulate repository pipeline for non-admin user
            var query = ReportIssue.VisibleToUser(reports, isAdmin: false);
            query = ReportIssue.OrderLatestFirst(query);
            var result = query.ToList();

            // Assert:
            // 1. Only approved reports remain
            Assert.That(result.Count, Is.EqualTo(2));

            // 2. Newest approved report appears first
            Assert.That(result[0].Id, Is.EqualTo(2));

            // 3. ImageUrl is preserved correctly for modal display
            Assert.That(result[0].ImageUrl, Is.EqualTo("https://example.com/new.jpg"));
        }
    }
}
