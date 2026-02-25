using InfrastructureApp.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InfrastructureApp_Tests
{
    // Feature83 - Tests for Latest Reports search filtering logic
    // Focus: keyword search + compatibility with existing visibility + sorting rules.
    public class ReportsLatestSearchTests
    {
        // -------------------------------------------------------
        // TEST 1: Keyword search matches description text
        // -------------------------------------------------------
        [Test]
        public void FilterByDescription_WhenKeywordMatches_ReturnsOnlyMatchingReports()
        {
            // Arrange
            var reports = new List<ReportIssue>
            {
                new ReportIssue { Description = "Large pothole on 4th St." },
                new ReportIssue { Description = "Faded crosswalk near school" },
                new ReportIssue { Description = "Small pothole near school" }
            }.AsQueryable();

            // Act
            var result = ReportIssue.FilterByDescription(reports, "pothole").ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(r => r.Description!.ToLower().Contains("pothole")), Is.True);
        }

        // -------------------------------------------------------
        // TEST 2: Keyword search trims whitespace
        // (Only include this if your FilterByDescription trims)
        // -------------------------------------------------------
        [Test]
        public void FilterByDescription_WhenKeywordHasWhitespace_StillMatches()
        {
            // Arrange
            var reports = new List<ReportIssue>
            {
                new ReportIssue { Description = "Streetlight out on Main" },
                new ReportIssue { Description = "Graffiti on bridge" }
            }.AsQueryable();

            // Act
            var result = ReportIssue.FilterByDescription(reports, "  streetlight  ").ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Description, Does.Contain("Streetlight"));
        }

        // -------------------------------------------------------
        // TEST 3: No matches => empty list
        // -------------------------------------------------------
        [Test]
        public void FilterByDescription_WhenNoMatches_ReturnsEmpty()
        {
            // Arrange
            var reports = new List<ReportIssue>
            {
                new ReportIssue { Description = "Broken stop sign" },
                new ReportIssue { Description = "Cracked sidewalk" }
            }.AsQueryable();

            // Act
            var result = ReportIssue.FilterByDescription(reports, "pothole").ToList();

            // Assert
            Assert.That(result, Is.Empty);
        }

        // -------------------------------------------------------
        // TEST 4: Full pipeline still works after searching:
        // Non-admin => Approved-only, still newest-first after filtering
        // -------------------------------------------------------
        [Test]
        public void SearchPipeline_WhenNotAdmin_FiltersByKeyword_ApprovedOnly_AndNewestFirst()
        {
            // Arrange
            var reports = new List<ReportIssue>
            {
                new ReportIssue
                {
                    Id = 1,
                    Status = "Approved",
                    Description = "Pothole on 4th St.",
                    CreatedAt = new DateTime(2026, 2, 1),
                    ImageUrl = "img1"
                },
                new ReportIssue
                {
                    Id = 2,
                    Status = "Pending",
                    Description = "Pothole on 4th St.",
                    CreatedAt = new DateTime(2026, 3, 1),
                    ImageUrl = "img2"
                },
                new ReportIssue
                {
                    Id = 3,
                    Status = "Approved",
                    Description = "Pothole on 4th St.",
                    CreatedAt = new DateTime(2026, 4, 1),
                    ImageUrl = "img3"
                },
                new ReportIssue
                {
                    Id = 4,
                    Status = "Approved",
                    Description = "Graffiti on wall",
                    CreatedAt = new DateTime(2026, 5, 1),
                    ImageUrl = "img4"
                }
            }.AsQueryable();

            // Act: simulate the repo/service pipeline for search (non-admin)
            var q = ReportIssue.VisibleToUser(reports, isAdmin: false);          // Approved-only
            q = ReportIssue.FilterByDescription(q, "pothole");                  // keyword filter
            q = ReportIssue.OrderLatestFirst(q);                                // newest-first
            var result = q.ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));                           // only approved potholes remain
            Assert.That(result.All(r => r.Status == "Approved"), Is.True);
            Assert.That(result[0].Id, Is.EqualTo(3));                           // newest approved pothole first
            Assert.That(result[1].Id, Is.EqualTo(1));
            Assert.That(result[0].ImageUrl, Is.EqualTo("img3"));                // modal needs this preserved
        }
    }
}