//this test verifies that the action renders a view and doesn't redirect or give an error. 

using InfrastructureApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Controllers
{
    [TestFixture]
    public class NearbyIssueControllerTests
    {
        [Test]
        public void Index_ReturnsViewResult()
        {
            // Arrange
            var controller = new NearbyIssueController();

            // Act
            var result = controller.Index();

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
    }
}