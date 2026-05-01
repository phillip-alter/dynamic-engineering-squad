using InfrastructureApp.Models;
using InfrastructureApp.Data;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using Reqnroll;
using Microsoft.Extensions.DependencyInjection;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using NUnit.Framework;
using OpenQA.Selenium.Support.UI;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class BanUserSteps : SeleniumTestBase
    {
        private readonly ScenarioContext _scenarioContext;

        public BanUserSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [Given(@"""(.*)"" is banned for ""(.*)""")]
        public async Task GivenIsBannedFor(string username, string reason)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var user = await userManager.FindByNameAsync(username);
            Assert.That(user, Is.Not.Null);

            user.IsBanned = true;
            user.BanReason = reason;
            await userManager.UpdateAsync(user);
        }

        [Then(@"I should see a ""Ban"" button for user ""(.*)""")]
        public void ThenIShouldSeeABanButtonForUser(string username)
        {
            EnsureUserIsVisibleOnAdminPage(username);
            var banButtons = Driver.FindElements(By.XPath($"//tr[td[contains(normalize-space(), '{username}')]]//button[contains(normalize-space(), 'Ban')]"));
            Assert.That(banButtons.Count, Is.GreaterThan(0), $"Ban button for user {username} not found.");
        }

        [When(@"I click ""Ban"" for user ""(.*)""")]
        public void WhenIClickBanForUser(string username)
        {
            EnsureUserIsVisibleOnAdminPage(username);
            var banButton = Driver.FindElement(By.XPath($"//tr[td[contains(normalize-space(), '{username}')]]//button[contains(normalize-space(), 'Ban')]"));
            ScrollAndClick(banButton);
        }

        [Then(@"I should see a ban confirmation modal for ""(.*)""")]
        public void ThenIShouldSeeABanConfirmationModalFor(string username)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            var modal = wait.Until(d => {
                var m = d.FindElement(By.Id("banModal"));
                return m.Displayed ? m : null;
            });
            Assert.That(modal.Text, Does.Contain($"Ban {username}"));
        }

        [When(@"I enter ""(.*)"" as the ban reason")]
        public void WhenIEnterAsTheBanReason(string reason)
        {
            var reasonInput = Driver.FindElement(By.Id("BanReason"));
            reasonInput.SendKeys(reason);
        }

        [When(@"I confirm the ban")]
        public void WhenIConfirmTheBan()
        {
            var confirmButton = Driver.FindElement(By.Id("confirmBanButton"));
            confirmButton.Click();
        }

        [Then(@"I should see ""Unban"" instead of ""Ban"" for user ""(.*)""")]
        public void ThenIShouldSeeUnbanInsteadOfBanForUser(string username)
        {
            EnsureUserIsVisibleOnAdminPage(username);
            var unbanButtons = Driver.FindElements(By.XPath($"//tr[td[contains(normalize-space(), '{username}')]]//button[contains(normalize-space(), 'Unban')]"));
            Assert.That(unbanButtons.Count, Is.GreaterThan(0), $"Unban button for user {username} not found.");
            
            var banButtons = Driver.FindElements(By.XPath($"//tr[td[contains(normalize-space(), '{username}')]]//button[contains(normalize-space(), 'Ban')]"));
            Assert.That(banButtons.Count, Is.EqualTo(0), $"Ban button for user {username} should not be present.");
        }

        [When(@"""(.*)"" bans ""(.*)"" for ""(.*)""")]
        public async Task WhenBansFor(string adminUsername, string targetUsername, string reason)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            
            var targetUser = await userManager.FindByNameAsync(targetUsername);
            var adminUser = await userManager.FindByNameAsync(adminUsername);

            Assert.That(targetUser, Is.Not.Null);
            Assert.That(adminUser, Is.Not.Null);

            await userService.BanUserAsync(targetUser.Id, adminUser.Id, reason);
        }

        [When(@"I click ""Unban"" for user ""(.*)""")]
        public void WhenIClickUnbanForUser(string username)
        {
            EnsureUserIsVisibleOnAdminPage(username);
            var unbanButton = Driver.FindElement(By.XPath($"//tr[td[contains(normalize-space(), '{username}')]]//button[contains(normalize-space(), 'Unban')]"));
            ScrollAndClick(unbanButton);
        }

        [Then("I should see an error message {string}")]
        [Scope(Feature = "Ban User")]
        public void ThenIShouldSeeAnErrorMessage(string expectedMessage)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            try
            {
                wait.Until(d => d.FindElement(By.TagName("body")).Text.Contains(expectedMessage));
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"Error message '{expectedMessage}' not found. Current URL: {Driver.Url}");
                Console.WriteLine("Page Source:");
                Console.WriteLine(Driver.PageSource);
                throw;
            }
            var bodyText = Driver.FindElement(By.TagName("body")).Text;
            Assert.That(bodyText, Does.Contain(expectedMessage));
        }

        [Then(@"a moderation action should be logged for ""(.*)"" ""(.*)""")]
        [Scope(Feature = "Ban User")]
        public async Task ThenAModerationActionShouldBeLoggedFor(string action, string username)
        {
             var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
             wait.Until(async d => {
                 using var scope = ServerHost!.Services.CreateScope();
                 var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                 return await db.ModerationActionLogs.AnyAsync(l => l.Action == action && l.TargetContentSnapshot.Contains(username));
             });
        }

        private void EnsureUserIsVisibleOnAdminPage(string username)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(45));
            while (true)
            {
                var userRows = Driver.FindElements(By.XPath($"//tr[td[contains(normalize-space(), '{username}')]]"));
                if (userRows.Count > 0) return;

                var nextButtons = Driver.FindElements(By.XPath("//li[contains(@class, 'page-item') and not(contains(@class, 'disabled'))]/a[contains(text(), 'Next')]"));
                if (nextButtons.Count == 0)
                {
                    break;
                }
                ScrollAndClick(nextButtons[0]);
                Thread.Sleep(500); // Wait for page load
            }
        }
    }
}
