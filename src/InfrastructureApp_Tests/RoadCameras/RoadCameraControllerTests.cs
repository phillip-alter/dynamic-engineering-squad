using System.Collections.Generic;
using System.Threading.Tasks;
using InfrastructureApp.Controllers;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace InfrastructureApp.Tests.RoadCameras
{
    [TestFixture]
    public class RoadCameraControllerTests
    {
        [Test]
        public async Task Index_WhenServiceReturnsCameras_ReturnsViewWithModel()
        {
            // Arrange
            var service = new Mock<ITripCheckService>();

            service.Setup(s => s.GetCamerasAsync())
                   .ReturnsAsync(new List<RoadCameraViewModel>
                   {
                       new RoadCameraViewModel { CameraId = "C001", Name = "Cam 1" }
                   });

            var controller = new RoadCameraController(service.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);

            var model = view!.Model as RoadCameraIndexViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Cameras.Count, Is.EqualTo(1));
            Assert.That(model.ErrorMessage, Is.Null);
        }

        [Test]
        public async Task Index_WhenServiceReturnsEmpty_ShowsFriendlyErrorMessage()
        {
            // Arrange
            var service = new Mock<ITripCheckService>();
            service.Setup(s => s.GetCamerasAsync())
                   .ReturnsAsync(new List<RoadCameraViewModel>());

            var controller = new RoadCameraController(service.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);

            var model = view!.Model as RoadCameraIndexViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Cameras.Count, Is.EqualTo(0));
            Assert.That(model.ErrorMessage, Is.EqualTo("Road camera data is temporarily unavailable."));
        }

        [Test]
        public async Task Details_WhenCameraFound_ReturnsViewWithCamera()
        {
            // Arrange
            var service = new Mock<ITripCheckService>();
            service.Setup(s => s.GetCameraByIdAsync("C001"))
                   .ReturnsAsync(new RoadCameraViewModel { CameraId = "C001", Name = "Cam 1" });

            var controller = new RoadCameraController(service.Object);

            // Act
            var result = await controller.Details("C001");

            // Assert
            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);

            var model = view!.Model as RoadCameraDetailsViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Camera.CameraId, Is.EqualTo("C001"));
            Assert.That(model.ErrorMessage, Is.Null);
        }

        [Test]
        public async Task Details_WhenCameraMissing_ReturnsViewWithFriendlyError()
        {
            // Arrange
            var service = new Mock<ITripCheckService>();
            service.Setup(s => s.GetCameraByIdAsync("BAD"))
                   .ReturnsAsync((RoadCameraViewModel?)null);

            var controller = new RoadCameraController(service.Object);

            // Act
            var result = await controller.Details("BAD");

            // Assert
            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);

            var model = view!.Model as RoadCameraDetailsViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Camera, Is.Null);
            Assert.That(model.ErrorMessage, Is.EqualTo("Road camera data is temporarily unavailable."));
        }

        [Test]
        public async Task RefreshImage_ReturnsJson_WithImageUrlAndTimestamp()
        {
            // Arrange
            var service = new Mock<ITripCheckService>();
            service.Setup(s => s.GetCameraByIdAsync("C001"))
                   .ReturnsAsync(new RoadCameraViewModel
                   {
                       CameraId = "C001",
                       ImageUrl = "https://example.com/new.jpg",
                       LastUpdated = System.DateTimeOffset.Parse("2026-02-17T15:30:00Z")
                   });

            var controller = new RoadCameraController(service.Object);

            // Act
            var result = await controller.RefreshImage("C001");

            // Assert
            var json = result as JsonResult;
            Assert.That(json, Is.Not.Null);

            // We’ll assert via anonymous object shape
            var dict = json!.Value as IDictionary<string, object>;
            Assert.That(dict, Is.Not.Null);

            Assert.That(dict!["cameraId"], Is.EqualTo("C001"));
            Assert.That(dict!["imageUrl"], Is.EqualTo("https://example.com/new.jpg"));
            Assert.That(dict!["lastUpdated"], Is.Not.Null);
        }
    }
}
