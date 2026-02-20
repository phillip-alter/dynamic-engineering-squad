using InfrastructureApp.Controllers;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace InfrastructureApp_Tests
{
    // Tests for DashboardController.
    // Verifies the controller returns a view and calls the repository.
    [TestFixture]
    public class DashboardControllerTests
    {
        // Ensures Index() returns a ViewResult with a DashboardViewModel.
        [Test]
        public async Task Index_ReturnsViewResult_WithDashboardViewModel()
        {
            // Arrange - Create a mock repository that returns known dashboard data.
            var repoMock = new Mock<IDashboardRepository>();

            repoMock
                .Setup(r => r.GetDashboardSummaryAsync())
                .ReturnsAsync(new DashboardViewModel
                {
                    Username = "DemoUser",
                    Email = "demo@example.com",
                    ReportsSubmitted = 0,
                    Points = 0
                });

            // Inject the mocked repository into the controller.
            var controller = new DashboardController(repoMock.Object);

            // Act - // Call the Index action method.
            var result = await controller.Index();

            // Assert 
            Assert.That(result, Is.TypeOf<ViewResult>());

            // Verify the model passed to the view is a DashboardViewModel.
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.TypeOf<DashboardViewModel>());

            // Verify the model contains the expected values.
            var model = (DashboardViewModel)viewResult.Model!;
            Assert.That(model.Username, Is.EqualTo("DemoUser"));
            Assert.That(model.Email, Is.EqualTo("demo@example.com"));
            Assert.That(model.ReportsSubmitted, Is.EqualTo(0));
            Assert.That(model.Points, Is.EqualTo(0));
        }

        // Ensures Index() calls the repository once.
        [Test]
        public async Task Index_ShouldCallRepositoryOnce_WhenInvoked()
        {
            // Arrange
            var repoMock = new Mock<IDashboardRepository>();

            repoMock
                .Setup(r => r.GetDashboardSummaryAsync())
                .ReturnsAsync(new DashboardViewModel());

            var controller = new DashboardController(repoMock.Object);

            // Act
            await controller.Index();

            // Assert -  Ensure the repository method was called once.
            repoMock.Verify(r => r.GetDashboardSummaryAsync(), Times.Once);
        }
    }
}