using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class LeaderboardControllerTests
    {
        private Mock<LeaderboardService>? _serviceMock;

        [SetUp]
        public void SetUp()
        {
            var repoDummy = Mock.Of<ILeaderboardRepository>();
            _serviceMock = new Mock<LeaderboardService>(repoDummy) { CallBase = false };
        }

        private LeaderboardController CreateController()
            => new LeaderboardController(_serviceMock!.Object);

        [Test]
        public async Task Index_ReturnsViewResult()
        {
            _serviceMock!.Setup(s => s.GetTopAsync(It.IsAny<int>()))
                         .ReturnsAsync(new List<LeaderboardEntry>());

            var controller = CreateController();

            var result = await controller.Index();

            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task Index_CallsService_GetTopAsync_Once_WithTopN()
        {
            const int topN = 25;

            _serviceMock!.Setup(s => s.GetTopAsync(topN))
                         .ReturnsAsync(new List<LeaderboardEntry>());

            var controller = CreateController();

            await controller.Index(topN);

            _serviceMock.Verify(s => s.GetTopAsync(topN), Times.Once);
        }

        [Test]
        public async Task Index_ReturnsViewModel_WithEntries_AndTopN()
        {
            const int topN = 10;

            var entries = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { UserId = "erin", UserPoints = 50, UpdatedAtUtc = DateTime.UtcNow },
                new LeaderboardEntry { UserId = "julian", UserPoints = 40, UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-2) }
            };

            _serviceMock!.Setup(s => s.GetTopAsync(topN))
                         .ReturnsAsync(entries);

            var controller = CreateController();

            var actionResult = await controller.Index(topN);
            var view = actionResult as ViewResult;

            Assert.That(view, Is.Not.Null);

            var model = view!.Model as LeaderboardIndexViewModel;
            Assert.That(model, Is.Not.Null);

            Assert.That(model!.TopN, Is.EqualTo(topN));
            Assert.That(model.Entries.Count, Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-25)]
        public async Task Index_WhenTopNInvalid_DefaultsTopNTo25_InViewModel(int badTopN)
        {
            _serviceMock!.Setup(s => s.GetTopAsync(badTopN))
                         .ReturnsAsync(new List<LeaderboardEntry>());

            var controller = CreateController();

            var actionResult = await controller.Index(badTopN);
            var view = actionResult as ViewResult;
            var model = view!.Model as LeaderboardIndexViewModel;

            Assert.That(model, Is.Not.Null);
            Assert.That(model!.TopN, Is.EqualTo(25));
        }
    }
}

