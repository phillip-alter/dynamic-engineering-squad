using InfrastructureApp.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace InfrastructureApp_Tests
{
    // not finished
    public class Tests
    {
        // Runs before each test
        [SetUp]
        public void Setup()
        {
            // Nothing needed here yet
        }

        // -------------------------------------------------------
        // TEST 1: Approved-only filtering logic
        // -------------------------------------------------------
        [Test]
        public void ApprovedOnly_Should_Return_Only_Approved_Reports()
        {
            // Arrange: create fake in-memory data
            var reports = new List<ReportIssue>
            {
                new ReportIssue { Status = "Approved" },
                new ReportIssue { Status = "Pending" },
                new ReportIssue { Status = "Rejected" },
                new ReportIssue { Status = "Approved" }
            }.AsQueryable();

            // Act: apply model filtering logic
            //var result = ReportIssue.ApprovedOnly(reports).ToList();

            // Assert: only approved should remain
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(r => r.Status == "Approved"), Is.True);
        }

        // -------------------------------------------------------
        // TEST 2: Newest-first sorting logic
        // -------------------------------------------------------
        [Test]
        public void NewestFirst_Should_Order_By_CreatedAt_Descending()
        {
            // Arrange
            var older = new ReportIssue { CreatedAt = new DateTime(2026, 1, 1) };
            var newer = new ReportIssue { CreatedAt = new DateTime(2026, 2, 1) };

            var reports = new List<ReportIssue> { older, newer }.AsQueryable();

            // Act
            var result = ReportIssue.NewestFirst(reports).ToList();

            // Assert
            Assert.That(result[0].CreatedAt, Is.EqualTo(newer.CreatedAt));
            Assert.That(result[1].CreatedAt, Is.EqualTo(older.CreatedAt));
        }

        // -------------------------------------------------------
        // TEST 3: Model validation - Required Description
        // -------------------------------------------------------
        [Test]
        public void ReportIssue_Should_Fail_When_Description_Is_Missing()
        {
            // Arrange
            var report = new ReportIssue
            {
                Description = "", // Required
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
            Assert.That(results.Any(r => r.MemberNames.Contains(nameof(ReportIssue.Description))), Is.True);
        }

        // -------------------------------------------------------
        // TEST 4: Model validation - Latitude range
        // -------------------------------------------------------
        [Test]
        public void ReportIssue_Should_Fail_When_Latitude_Out_Of_Range()
        {
            // Arrange
            var report = new ReportIssue
            {
                Description = "Test report",
                Status = "Approved",
                UserId = "user-guid-001",
                Latitude = 200 // invalid range
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
            Assert.That(results.Any(r =>
                r.MemberNames.Contains(nameof(ReportIssue.Latitude))), Is.True);
        }
    }
}