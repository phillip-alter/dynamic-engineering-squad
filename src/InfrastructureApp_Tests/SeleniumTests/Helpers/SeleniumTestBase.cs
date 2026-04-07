using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using NUnit.Framework;

namespace InfrastructureApp_Tests.SeleniumTests.Helpers
{
    public abstract class SeleniumTestBase
    {
        protected IWebDriver Driver = null!;
        protected const string BaseUrl = "http://localhost:5044";

        protected const string TestUsername = "ErinBleu";
        protected const string TestPassword = "Password1234!";

        [SetUp]
        public void SetUpDriver()
        {

            try { Driver?.Quit(); } catch { } 

            var options = new ChromeOptions();
            options.AddArgument("--headless");        // runs without opening a browser window
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--window-size=1280,900");

            Driver = new ChromeDriver(options);
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }

       [TearDown]
        public void TearDownDriver()
        {
            try
            {
                Driver?.Quit();
                Driver?.Dispose();
            }
            catch
            {
                // ignore teardown errors so they don't affect other tests
            }
            finally
            {
                Driver = null!;
            }
        }

        protected void Login(string username = TestUsername, string password = TestPassword)
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");

            Driver.FindElement(By.Id("UserName")).SendKeys(username);
            Driver.FindElement(By.Id("Password")).SendKeys(password);
            Driver.FindElement(By.CssSelector("input[type='submit']")).Click();

        

            // Wait until redirected away from login page
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => !d.Url.Contains("/Account/Login"));
        }
    }
}