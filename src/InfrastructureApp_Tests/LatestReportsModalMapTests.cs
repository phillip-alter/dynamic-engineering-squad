using InfrastructureApp.Controllers.API;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class LatestReportsModalMapTests
    {
        // Mock repository used to simulate data access without calling the real database
        private IReportIssueRepository _repo = null!;

        // Mock logger required by the controller constructor
        private ILogger<ReportsAPIController> _logger = null!;

        // Instance of the API controller being tested
        private ReportsAPIController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            // Created a mock repository so the controller can be tested without calling the real database
            _repo = Substitute.For<IReportIssueRepository>();

            // Created a mock logger since the controller expects a logger dependency
            _logger = Substitute.For<ILogger<ReportsAPIController>>();

            // Create the controller instance using the mocked dependencies
            _controller = new ReportsAPIController(_repo, _logger);

            // Provide a minimal request environment so the controller behaves
            // like it is handling a real web request
            _controller.ControllerContext = new ControllerContext
            {
                // Set a basic request context so the controller behaves like it is handling
                // a real API request during the test
                HttpContext = new DefaultHttpContext()
            };
        }
    }
}