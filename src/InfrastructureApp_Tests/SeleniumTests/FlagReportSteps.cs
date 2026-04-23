using System;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class FlagReportSteps : SeleniumTestBase
    {
        private int _reportId;

        [Given(@"a report exists with description ""(.*)""")]
        public async Task GivenAReportExistsWithDescription(string description)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var report = new ReportIssue
            {
                Description = description,
                Status = "Approved",
                UserId = "selenium-user",
                CreatedAt = DateTime.UtcNow
            };

            db.ReportIssue.Add(report);
            await db.SaveChangesAsync();
            _reportId = report.Id;
        }

        [Given(@"I am logged in as ""(.*)""")]
        public async Task GivenIAmLoggedInAs(string username)
        {
            await CreateTestUser(username, "Password123!");
            Login(username, "Password123!");
        }

        [When(@"I navigate to that report's details page")]
        public void WhenINavigateToThatReportsDetailsPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/ReportIssue/Details/{_reportId}");
        }

        [Then(@"I should see a ""Flag"" icon")]
        public void ThenIShouldSeeAFlagIcon()
        {
            var flagBtn = Driver.FindElement(By.Id("flagBtn"));
            Assert.That(flagBtn.Displayed, Is.True);
        }

        [When(@"I click the ""Flag"" icon")]
        [Given(@"I have clicked the ""Flag"" icon")]
        public void WhenIClickTheFlagIcon()
        {
            var flagBtn = Driver.FindElement(By.Id("flagBtn"));
            ScrollAndClick(flagBtn);
            
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.FindElement(By.Id("flagModal")).Displayed);
        }

        [Then(@"I should be presented with categories ""(.*)"", ""(.*)"", ""(.*)""")]
        public void ThenIShouldBePresentedWithCategories(string cat1, string cat2, string cat3)
        {
            var body = Driver.FindElement(By.Id("flagModal")).Text;
            Assert.Multiple(() =>
            {
                Assert.That(body, Does.Contain(cat1));
                Assert.That(body, Does.Contain(cat2));
                Assert.That(body, Does.Contain(cat3));
            });
        }

        [When(@"I select category ""(.*)""")]
        public void WhenISelectCategory(string category)
        {
            var radio = Driver.FindElement(By.XPath($"//label[contains(text(), '{category}')]/preceding-sibling::input"));
            radio.Click();
        }

        [When(@"I click ""Submit Report""")]
        public void WhenIClickSubmitReport()
        {
            var submitBtn = Driver.FindElement(By.Id("submitFlagBtn"));
            submitBtn.Click();
        }

        [Then(@"I should see a confirmation message ""(.*)""")]
        public void ThenIShouldSeeAConfirmationMessage(string expectedMessage)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var messageEl = wait.Until(d => {
                var el = d.FindElement(By.Id("flagMessage"));
                return el.Displayed && el.Text.Contains(expectedMessage) ? el : null;
            });
            Assert.That(messageEl, Is.Not.Null);
        }

        [Then(@"the reporting interface should close")]
        public void ThenTheReportingInterfaceShouldClose()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => !d.FindElement(By.Id("flagModal")).Displayed);
            Assert.That(Driver.FindElement(By.Id("flagModal")).Displayed, Is.False);
        }

        [Then(@"the ""Flag"" icon should be disabled and show ""Already Flagged""")]
        public void ThenTheFlagIconShouldBeDisabledAndShowAlreadyFlagged()
        {
            var flagBtn = Driver.FindElement(By.Id("flagBtn"));
            Assert.Multiple(() =>
            {
                Assert.That(flagBtn.Enabled, Is.False);
                Assert.That(flagBtn.Text, Does.Contain("Already Flagged"));
            });
        }

        [Given(@"I have already flagged that report with category ""(.*)""")]
        public async Task GivenIHaveAlreadyFlaggedThatReportWithCategory(string category)
        {
            await GivenIAmLoggedInAs("testuser");
            
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await userManager.FindByNameAsync("testuser");

            db.ReportFlags.Add(new ReportFlag
            {
                ReportIssueId = _reportId,
                UserId = user!.Id,
                Category = category,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }
}
