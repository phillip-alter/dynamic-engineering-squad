using InfrastructureApp.Models;
using InfrastructureApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Microsoft.Extensions.DependencyInjection;
using InfrastructureApp_Tests.SeleniumTests.Helpers;
using NUnit.Framework;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class SearchUsersSteps : SeleniumTestBase
    {
        [Given(@"a user with username ""(.*)"" exists")]
        public async Task GivenAUserWithUsernameExists(string username)
        {
            await CreateTestUser(username, "Password123!");
        }

        [Then(@"I should see a search input field")]
        public void ThenIShouldSeeASearchInputField()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var searchInput = wait.Until(d => d.FindElement(By.Id("userSearchInput")));
            Assert.That(searchInput.Displayed, Is.True);
        }

        [When(@"I enter ""(.*)"" into the search field")]
        public void WhenIEnterIntoTheSearchField(string searchTerm)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var searchInput = wait.Until(d => d.FindElement(By.Id("userSearchInput")));
            searchInput.Clear();
            searchInput.SendKeys(searchTerm);
            
            // Wait for debounce and reload
            Thread.Sleep(1000); 
        }

        [Then(@"I should see ""(.*)"" in the user list")]
        public void ThenIShouldSeeInTheUserList(string username)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.TagName("table")));
            var userRows = Driver.FindElements(By.XPath($"//tr[td[contains(normalize-space(), '{username}')]]"));
            Assert.That(userRows.Count, Is.GreaterThan(0), $"User {username} not found in user list.");
        }

        [Then(@"I should not see ""(.*)"" in the user list")]
        public void ThenIShouldNotSeeInTheUserList(string username)
        {
            var userRows = Driver.FindElements(By.XPath($"//tr[td[contains(normalize-space(), '{username}')]]"));
            Assert.That(userRows.Count, Is.EqualTo(0), $"User {username} found in user list but should not be.");
        }

        [Then(@"I should see an empty state message ""(.*)""")]
        public void ThenIShouldSeeAnEmptyStateMessage(string expectedMessage)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            var emptyMessage = wait.Until(d => d.FindElement(By.CssSelector(".alert-info")));
            Assert.That(emptyMessage.Text, Does.Contain(expectedMessage));
        }

        [When(@"I clear the search field")]
        public void WhenIClearTheSearchField()
        {
            var clearBtn = Driver.FindElement(By.LinkText("Clear"));
            clearBtn.Click();
            Thread.Sleep(500);
        }
    }
}
