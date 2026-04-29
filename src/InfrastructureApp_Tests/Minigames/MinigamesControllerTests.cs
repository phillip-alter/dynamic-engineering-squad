using System.Security.Claims;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services.Minigames;
using InfrastructureApp.ViewModels.Minigames;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InfrastructureApp_Tests.Minigames
{
    [TestFixture]
    public class MinigamesControllerTests
    {
        [Test]
        public async Task Index_ReturnsViewResult_WithGameCards()
        {
            var serviceMock = new Mock<IMinigameService>();
            serviceMock
                .Setup(service => service.GetTodayStatusesAsync("user-1", null))
                .ReturnsAsync(new[]
                {
                    new MinigameStatus { GameKey = MinigameConstants.SlotsGameKey, DailyPointsEarned = 2, DailyPointsLimit = 5, HasReachedDailyLimit = false },
                    new MinigameStatus { GameKey = MinigameConstants.MatchingGameKey, DailyPointsEarned = 0, DailyPointsLimit = 5, HasReachedDailyLimit = false },
                    new MinigameStatus { GameKey = MinigameConstants.TriviaGameKey, DailyPointsEarned = 0, DailyPointsLimit = 5, HasReachedDailyLimit = false },
                    new MinigameStatus { GameKey = MinigameConstants.TapRepairGameKey, DailyPointsEarned = 0, DailyPointsLimit = 5, HasReachedDailyLimit = false }
                });
            serviceMock
                .Setup(service => service.GetCurrentPointsAsync("user-1"))
                .ReturnsAsync(25);

            var controller = new MinigamesController(serviceMock.Object, CreateUserManager())
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.Index();

            Assert.That(result, Is.TypeOf<ViewResult>());

            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.TypeOf<MinigamesIndexViewModel>());

            var model = (MinigamesIndexViewModel)viewResult.Model!;
            Assert.That(model.CurrentPoints, Is.EqualTo(25));
            Assert.That(model.Games.Count, Is.EqualTo(4));
            Assert.That(model.Games.Single(game => game.GameKey == MinigameConstants.SlotsGameKey).IsAvailable, Is.True);
            Assert.That(model.Games.Single(game => game.GameKey == MinigameConstants.SlotsGameKey).DailyPointsEarned, Is.EqualTo(2));
            Assert.That(model.Games.Single(game => game.GameKey == MinigameConstants.MatchingGameKey).IsAvailable, Is.True);
        }

        [Test]
        public async Task Matching_ReturnsViewResult_WithMatchingViewModel()
        {
            var serviceMock = new Mock<IMinigameService>();
            serviceMock
                .Setup(service => service.GetTodayStatusesAsync("user-1", null))
                .ReturnsAsync(new[]
                {
                    new MinigameStatus { GameKey = MinigameConstants.MatchingGameKey, DailyPointsEarned = 0, DailyPointsLimit = 5, HasReachedDailyLimit = false }
                });
            serviceMock
                .Setup(service => service.GetCurrentPointsAsync("user-1"))
                .ReturnsAsync(31);

            var controller = new MinigamesController(serviceMock.Object, CreateUserManager())
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.Matching();

            Assert.That(result, Is.TypeOf<ViewResult>());

            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.TypeOf<MatchingViewModel>());

            var model = (MatchingViewModel)viewResult.Model!;
            Assert.That(model.CurrentPoints, Is.EqualTo(31));
            Assert.That(model.HasReachedDailyLimit, Is.False);
        }

        [Test]
        public async Task SpinSlots_ReturnsJsonWithServerGeneratedSymbols()
        {
            var serviceMock = new Mock<IMinigameService>();
            serviceMock
                .Setup(service => service.SpinSlotsAsync("user-1", null))
                .ReturnsAsync(new SlotsSpinResult
                {
                    GameKey = MinigameConstants.SlotsGameKey,
                    AwardedPoints = 1,
                    CurrentPoints = 15,
                    DailyPointsEarned = 3,
                    DailyPointsLimit = 5,
                    HasReachedDailyLimit = false,
                    Symbols = new[] { "cone", "bridge", "pothole" },
                    IsWinningSpin = true,
                    ResultLabel = "Three of a Kind"
                });

            var controller = new MinigamesController(serviceMock.Object, CreateUserManager())
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.SpinSlots();

            Assert.That(result, Is.TypeOf<JsonResult>());

            var jsonResult = (JsonResult)result;
            Assert.That(jsonResult.Value, Is.TypeOf<GameCompletionResultViewModel>());

            var payload = (GameCompletionResultViewModel)jsonResult.Value!;
            Assert.That(payload.Symbols, Is.EquivalentTo(new[] { "cone", "bridge", "pothole" }));
            Assert.That(payload.AwardedPoints, Is.EqualTo(1));
        }

        [Test]
        public async Task SpinSlots_WhenDailyCapReached_ReturnsNoAdditionalPoints()
        {
            var serviceMock = new Mock<IMinigameService>();
            serviceMock
                .Setup(service => service.SpinSlotsAsync("user-1", null))
                .ReturnsAsync(new SlotsSpinResult
                {
                    GameKey = MinigameConstants.SlotsGameKey,
                    AwardedPoints = 0,
                    CurrentPoints = 15,
                    DailyPointsEarned = 5,
                    DailyPointsLimit = 5,
                    HasReachedDailyLimit = true,
                    Symbols = new[] { "cone", "bridge", "pothole" },
                    IsWinningSpin = false,
                    ResultLabel = "Road Crew Spin"
                });

            var controller = new MinigamesController(serviceMock.Object, CreateUserManager())
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.SpinSlots();
            var payload = (GameCompletionResultViewModel)((JsonResult)result).Value!;

            Assert.That(payload.AwardedPoints, Is.EqualTo(0));
            Assert.That(payload.HasReachedDailyLimit, Is.True);
        }

        [Test]
        public async Task CompleteGame_WithInvalidGameKey_ReturnsBadRequest()
        {
            var serviceMock = new Mock<IMinigameService>();
            var controller = new MinigamesController(serviceMock.Object, CreateUserManager())
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.CompleteGame(new CompleteGameRequestViewModel
            {
                GameKey = "not-a-game"
            });

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CompleteGame_WithSlotsGameKey_ReturnsBadRequest()
        {
            var serviceMock = new Mock<IMinigameService>();
            var controller = new MinigamesController(serviceMock.Object, CreateUserManager())
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.CompleteGame(new CompleteGameRequestViewModel
            {
                GameKey = MinigameConstants.SlotsGameKey
            });

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CompleteGame_ForMatching_ReturnsCompletionPayload()
        {
            var serviceMock = new Mock<IMinigameService>();
            serviceMock
                .Setup(service => service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, null))
                .ReturnsAsync(new MinigameAwardResult
                {
                    GameKey = MinigameConstants.MatchingGameKey,
                    AwardedPoints = 1,
                    CurrentPoints = 36,
                    DailyPointsEarned = 1,
                    DailyPointsLimit = 5,
                    HasReachedDailyLimit = false
                });

            var controller = new MinigamesController(serviceMock.Object, CreateUserManager())
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.CompleteGame(new CompleteGameRequestViewModel
            {
                GameKey = MinigameConstants.MatchingGameKey
            });

            Assert.That(result, Is.TypeOf<JsonResult>());

            var payload = (GameCompletionResultViewModel)((JsonResult)result).Value!;
            Assert.That(payload.GameKey, Is.EqualTo(MinigameConstants.MatchingGameKey));
            Assert.That(payload.AwardedPoints, Is.EqualTo(1));
            Assert.That(payload.HasReachedDailyLimit, Is.False);
        }

        private static UserManager<Users> CreateUserManager()
        {
            var store = new Mock<IUserStore<Users>>();
            return new UserManager<Users>(
                store.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        private static ControllerContext BuildControllerContext(string userId)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "minigame-user")
            }, "TestAuth");

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }
    }
}
