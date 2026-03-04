using InfrastructureApp.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InfrastructureApp_Tests
{
    // Tests for date sorting and full sorting pipeline of Latest Reports
    public class ReportsLatestSortTests
    {
        [Test]
        public void ApplyDateSort_Default_ReturnsNewestFirst()
        {
            // Arrange: create two reports with different dates
            var a = new ReportIssue { Id = 1, CreatedAt = new DateTime(2026, 1, 1) };
            var b = new ReportIssue { Id = 2, CreatedAt = new DateTime(2026, 2, 1) };

            var reports = new List<ReportIssue> { a, b }.AsQueryable();

            // Act: apply default sort (should be newest first)
            var result = ReportIssue.ApplyDateSort(reports, sort: null).ToList();

            // Assert: newest report comes first
            Assert.That(result[0].Id, Is.EqualTo(2));
            Assert.That(result[1].Id, Is.EqualTo(1));
        }

        [Test]
        public void ApplyDateSort_WhenOldest_ReturnsOldestFirst()
        {
            // Arrange: create reports in reverse order
            var a = new ReportIssue { Id = 1, CreatedAt = new DateTime(2026, 1, 1) };
            var b = new ReportIssue { Id = 2, CreatedAt = new DateTime(2026, 2, 1) };

            var reports = new List<ReportIssue> { b, a }.AsQueryable();

            // Act: apply oldest-first sort
            var result = ReportIssue.ApplyDateSort(reports, sort: "oldest").ToList();

            // Assert: oldest report comes first
            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[1].Id, Is.EqualTo(2));
        }

        [Test]
        public void SortPipeline_WhenNotAdmin_ApprovedOnly_AndOldestFirst()
        {
            // Arrange: mix of Approved and Pending reports with same keyword
            var reports = new List<ReportIssue>
            {
                new ReportIssue { Id = 1, Status="Approved", Description="Pothole", CreatedAt = new DateTime(2026, 2, 1) },
                new ReportIssue { Id = 2, Status="Pending",  Description="Pothole", CreatedAt = new DateTime(2026, 1, 1) },
                new ReportIssue { Id = 3, Status="Approved", Description="Pothole", CreatedAt = new DateTime(2026, 3, 1) }
            }.AsQueryable();

            // Act: apply visibility, filter, and oldest-first sorting
            var q = ReportIssue.VisibleToUser(reports, isAdmin: false);
            q = ReportIssue.FilterByDescription(q, "Pothole");
            q = ReportIssue.ApplyDateSort(q, "oldest");

            var result = q.ToList();

            // Assert: only Approved reports and sorted oldest first
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Id, Is.EqualTo(1)); // Feb 1
            Assert.That(result[1].Id, Is.EqualTo(3)); // Mar 1
        }
    }
}