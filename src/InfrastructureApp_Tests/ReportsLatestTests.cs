using InfrastructureApp.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace InfrastructureApp_Tests
{
    // Tests for the "Latest Reports" feature logic.
    // Sprint 1 rule: unit test C# logic in models/repositories/view-models (not UI).
    public class ReportsLatestTests
    {
         // This runs before every test method
        // Useful later if shared test setup is needed
        [SetUp]
        public void Setup()
        {
            // No shared setup required
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
        // Uses the model helper: ReportIssue.OrderLatestFirst(...)
        // -------------------------------------------------------
        [Test]
        public void OrderLatestFirst_OrdersByCreatedAtDescending()
        {
            // Arrange
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
    }
}
