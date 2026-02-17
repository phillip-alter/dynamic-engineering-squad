using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using InfrastructureApp.Controllers;
using Moq;
using Microsoft.Extensions.Logging;

namespace InfrastructureApp_Tests
{
    public class AboutPageTest
    {
        [Test]
        public void About_ReturnsView()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(loggerMock.Object);

            // Act
            var result = controller.About();

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public void About_ViewName_IsCorrect()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(loggerMock.Object);

            // Act
            var view = controller.About() as ViewResult;

            // Assert
            Assert.That(view, Is.Not.Null);
            Assert.That(view!.ViewName == null || view.ViewName == "About");
        }
    }
}

