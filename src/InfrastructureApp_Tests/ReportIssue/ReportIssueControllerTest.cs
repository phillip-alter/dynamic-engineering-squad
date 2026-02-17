using System;
using System.Security.Claims;
using System.Threading.Tasks;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class ReportIssueControllerTests
    {
        //creates a fake service/fake substitute for users
        private IReportIssueService _service = null!;
        private UserManager<Users> _userManager = null!;

        [SetUp]
        public void SetUp()
        {
            _service = Substitute.For<IReportIssueService>();
            _userManager = MakeUserManager();
        }

        [TearDown]
        public void TearDown()
        {
            _userManager?.Dispose();
        }

        // -------------------------
        // Helpers
        // -------------------------

        private static UserManager<Users> MakeUserManager()
        {
            var store = Substitute.For<IUserStore<Users>>();
            // UserManager has a big ctor; pass null for dependencies you don't use.
            return Substitute.For<UserManager<Users>>(
                store,
                null, null, null, null, null, null, null, null);
        }

        private ReportIssueController MakeController(ClaimsPrincipal? user = null)
        {
            var controller = new ReportIssueController(_service, _userManager);

            // Set up HttpContext + User
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            if (user != null)
                controller.ControllerContext.HttpContext.User = user;

            // Set up TempData (so controller.TempData["Success"] works)
            controller.TempData = new TempDataDictionary(
                controller.ControllerContext.HttpContext,
                Substitute.For<ITempDataProvider>());

            return controller;
        }

        //This creates a fake authenticated user with the NameIdentifier claim, which is typically what Identity uses for user id.
        private static ClaimsPrincipal MakeUserPrincipal(string userId = "user-123")
        {
            // Note: Controller uses UserManager.GetUserId(User), which typically reads NameIdentifier.
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "testuser")
            }, authenticationType: "TestAuth");

            return new ClaimsPrincipal(identity);
        }

        // -------------------------
        // GET tests
        // -------------------------

        //Verifies the GET action returns a View (not redirect, not error). It doesn’t check view name or model, just the result type.
        [Test]
        public void ReportIssue_Get_ReturnsView()
        {
            var controller = MakeController();

            var result = controller.ReportIssue();

            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        //get returns a view which has a model and the model is a fresh reportIssueViewModel
        [Test]
        public void Create_Get_ReturnsView_WithNewViewModel()
        {
            var controller = MakeController();

            var result = controller.Create();

            Assert.That(result, Is.TypeOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.TypeOf<ReportIssueViewModel>());
        }

        // -------------------------
        // POST Create tests
        // -------------------------

        //if model validation fails, controller should short-circuit and re-show the form, not save anything.
        [Test]
        public async Task Create_Post_InvalidModelState_ReturnsView_DoesNotCallService()
        {
            var controller = MakeController();
            var vm = new ReportIssueViewModel();

            controller.ModelState.AddModelError("Description", "Required");

            var result = await controller.Create(vm);

            Assert.That(result, Is.TypeOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.SameAs(vm));

            await _service.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
        }

        //provide a fake user, return user id, return created report, tests flows + side effects
        [Test]
        public async Task Create_Post_Valid_RedirectsToDetails_SetsTempData_CallsService_WithAuthenticatedUserId()
        {
            // Arrange
            var principal = MakeUserPrincipal("user-abc");
            var controller = MakeController(principal);

            _userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns("user-abc");
            _service.CreateAsync(Arg.Any<ReportIssueViewModel>(), "user-abc").Returns(123);

            var vm = new ReportIssueViewModel(); // ModelState is valid unless you add errors in unit tests

            // Act
            var result = await controller.Create(vm);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;

            Assert.That(redirect.ActionName, Is.EqualTo(nameof(ReportIssueController.Details)));
            Assert.That(redirect.RouteValues, Is.Not.Null);
            Assert.That(redirect.RouteValues!["id"], Is.EqualTo(123));

            Assert.That(controller.TempData["Success"], Is.EqualTo("XP gained! +10 points awarded."));

            await _service.Received(1).CreateAsync(vm, "user-abc");
        }

        //when userId is null, goes to default user-guid-001, controller doesn't crash if no authenticated user
        [Test]
        public async Task Create_Post_WhenUserIdNull_UsesFallbackUserGuid001()
        {
            // Arrange
            var controller = MakeController(); // no user set
            _userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns((string?)null);

            _service.CreateAsync(Arg.Any<ReportIssueViewModel>(), "user-guid-001").Returns(55);

            var vm = new ReportIssueViewModel();

            // Act
            var result = await controller.Create(vm);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            await _service.Received(1).CreateAsync(vm, "user-guid-001");
        }


        //tests your controller’s error handling for a known/expected exception.
        [Test]
        public async Task Create_Post_WhenServiceThrowsInvalidOperation_AddsModelError_ReturnsView()
        {
            // Arrange
            var controller = MakeController();
            _userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns("user-123");

            _service.CreateAsync(Arg.Any<ReportIssueViewModel>(), Arg.Any<string>())
                    .Returns<Task<int>>(_ => Task.FromException<int>(
                        new InvalidOperationException("attach a photo")));

            var vm = new ReportIssueViewModel();

            // Act
            var result = await controller.Create(vm);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(controller.ModelState.IsValid, Is.False);

            Assert.That(controller.ModelState.ContainsKey(string.Empty), Is.True);

            var errors = controller.ModelState[string.Empty].Errors;
            Assert.That(errors, Is.Not.Empty);
            Assert.That(errors[0].ErrorMessage, Does.Contain("attach a photo"));
        }

        //tests your controller’s error handling for a unexpected exception.
        [Test]
        public async Task Create_Post_WhenServiceThrowsUnknown_AddsGenericModelError_ReturnsView()
        {
            // Arrange
            var controller = MakeController();
            _userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns("user-123");

            _service.CreateAsync(Arg.Any<ReportIssueViewModel>(), Arg.Any<string>())
                    .Returns<Task<int>>(_ => Task.FromException<int>(
                        new Exception("db down")));

            var vm = new ReportIssueViewModel();

            // Act
            var result = await controller.Create(vm);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(controller.ModelState.IsValid, Is.False);

            Assert.That(controller.ModelState.ContainsKey(string.Empty), Is.True);

            var errors = controller.ModelState[string.Empty].Errors;
            Assert.That(errors, Is.Not.Empty);
            Assert.That(errors[0].ErrorMessage,
                Is.EqualTo("Something went wrong saving your report. Please try again."));
        }

        // -------------------------
        // Details tests
        // -------------------------

        //if report not found, return not found
        [Test]
        public async Task Details_WhenReportNotFound_ReturnsNotFound()
        {
            var controller = MakeController();

            _service.GetByIdAsync(999).Returns((ReportIssue?)null);

            var result = await controller.Details(999);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        //verifying the controller passes the entity to the view
        [Test]
        public async Task Details_WhenReportFound_ReturnsView_WithModel()
        {
            var controller = MakeController();

            var report = new ReportIssue
            {
                Id = 7,
                Description = "Pothole",
                Status = "Pending",
                UserId = "user-1"
            };

            _service.GetByIdAsync(7).Returns(report);

            var result = await controller.Details(7);

            Assert.That(result, Is.TypeOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.SameAs(report));
        }
    }
}
