using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace InfrastructureApp_Tests.SeleniumTests;

[TestFixture]
[Category("Selenium")]
public class SocialSharingSeleniumTests : SeleniumTestBase
{
    private int _approvedReportId;
    private int _pendingReportId;

    [SetUp]
    public async Task CreateReports()
    {
        _approvedReportId = await CreateTestReport("Cracked pavement near the community centre", "Approved");
        _pendingReportId = await CreateTestReport("Suspected pothole near the school", "Pending");
    }

    private async Task<int> CreateTestReport(string description, string status)
    {
        using var scope = ServerHost!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var report = new ReportIssue
        {
            Description = description,
            Status = status,
            UserId = "selenium-test-user",
            CreatedAt = DateTime.UtcNow
        };

        db.ReportIssue.Add(report);
        await db.SaveChangesAsync();
        return report.Id;
    }

    // ── Unauthenticated ──────────────────────────────────────────────────

    [Test]
    public void ShareSection_WhenNotLoggedIn_IsNotVisible()
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_approvedReportId}");

        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.CssSelector(".report-card")));

        Assert.That(Driver.PageSource, Does.Not.Contain("Share this issue"));
    }

    // ── Authenticated / Approved ─────────────────────────────────────────

    [Test]
    public void ShareSection_WhenLoggedIn_WithApprovedReport_IsVisible()
    {
        Login();
        Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_approvedReportId}");

        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.CssSelector(".report-card")));

        Assert.That(Driver.PageSource, Does.Contain("Share this issue"));
    }

    [Test]
    public void FacebookShareButton_WhenLoggedIn_IsDisplayed()
    {
        Login();
        Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_approvedReportId}");

        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
        var btn = wait.Until(d => d.FindElement(By.CssSelector("a[aria-label='Share on Facebook']")));

        Assert.That(btn.Displayed, Is.True);
        Assert.That(btn.Text.Trim(), Does.Contain("Facebook"));
    }

    [Test]
    public void FacebookShareButton_HrefPointsToFacebookSharer()
    {
        Login();
        Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_approvedReportId}");

        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
        var btn = wait.Until(d => d.FindElement(By.CssSelector("a[aria-label='Share on Facebook']")));

        var href = btn.GetAttribute("href");
        Assert.That(href, Does.Contain("facebook.com/sharer"));
        Assert.That(href, Does.Contain(Uri.EscapeDataString($"{BaseUrl}/ReportIssue/Details/{_approvedReportId}")));
    }

    [Test]
    public void FacebookShareButton_OpensInNewTab()
    {
        Login();
        Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_approvedReportId}");

        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
        var btn = wait.Until(d => d.FindElement(By.CssSelector("a[aria-label='Share on Facebook']")));

        Assert.That(btn.GetAttribute("target"), Is.EqualTo("_blank"));
    }

    // ── Authenticated / Pending ──────────────────────────────────────────

    [Test]
    public void ShareSection_WhenLoggedIn_WithPendingReport_IsNotVisible()
    {
        Login();
        Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_pendingReportId}");

        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.CssSelector(".report-card")));

        Assert.That(Driver.PageSource, Does.Not.Contain("Share this issue"));
    }
}
