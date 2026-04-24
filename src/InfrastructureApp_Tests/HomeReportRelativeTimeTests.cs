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
    }
}
