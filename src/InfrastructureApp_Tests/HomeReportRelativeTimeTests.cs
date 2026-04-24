using InfrastructureApp.Models;
using NUnit.Framework;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class HomeReportRelativeTimeTests
    {
        // -------------------------------------------------------
        // TEST 1: Reports created less than one minute ago show "Just now"
        // -------------------------------------------------------
        [Test]
        public void BuildRelativeTime_LessThanOneMinuteAgo_ReturnsJustNow()
        {
            var createdAt = DateTime.UtcNow.AddSeconds(-30);

            var result = ReportIssue.BuildRelativeTime(createdAt, DateTime.UtcNow);

            Assert.That(result, Is.EqualTo("Just now"));
        }

        // -------------------------------------------------------
        // TEST 2: Future report timestamps show "Just now"
        // -------------------------------------------------------
        [Test]
        public void BuildRelativeTime_FutureTimestamp_ReturnsJustNow()
        {
            var currentTime = DateTime.UtcNow;
            var createdAt = currentTime.AddMinutes(5);

            var result = ReportIssue.BuildRelativeTime(createdAt, currentTime);

            Assert.That(result, Is.EqualTo("Just now"));
        }

        // -------------------------------------------------------
        // TEST 3: Reports created minutes ago show "X minutes ago"
        // -------------------------------------------------------
        [Test]
        public void BuildRelativeTime_FiveMinutesAgo_ReturnsFiveMinutesAgo()
        {
            var currentTime = DateTime.UtcNow;
            var createdAt = currentTime.AddMinutes(-5);

            var result = ReportIssue.BuildRelativeTime(createdAt, currentTime);

            Assert.That(result, Is.EqualTo("5 minutes ago"));
        }

        // -------------------------------------------------------
        // TEST 4: Reports created hours ago show "X hours ago"
        // -------------------------------------------------------
        [Test]
        public void BuildRelativeTime_TwoHoursAgo_ReturnsTwoHoursAgo()
        {
            var currentTime = DateTime.UtcNow;
            var createdAt = currentTime.AddHours(-2);

            var result = ReportIssue.BuildRelativeTime(createdAt, currentTime);

            Assert.That(result, Is.EqualTo("2 hours ago"));
        }
    }
}
