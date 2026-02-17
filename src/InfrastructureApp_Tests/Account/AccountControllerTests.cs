using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace InfrastructureApp_Tests;

[TestFixture]
public class AccountControllerTests
{
    private Mock<UserManager<Users>> _mockUserManager;
    private Mock<SignInManager<Users>> _mockSignInManager;
    private AccountController _controller;

    [SetUp]
    public void Setup()
    {
        var store = new Mock<IUserStore<Users>>();
        _mockUserManager = new Mock<UserManager<Users>>(store.Object, null, null, null, null, null, null, null, null);
        _mockSignInManager = new Mock<SignInManager<Users>>(
            _mockUserManager.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<Users>>().Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<ILogger<SignInManager<Users>>>().Object,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<Users>>().Object
        );
        _controller = new AccountController(_mockUserManager.Object, _mockSignInManager.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    [Test]
    public async Task RegisterPost_ReturnsView_WhenModelStateInvalid()
    {
        _controller.ModelState.AddModelError("Email", "Required");
        var model = new RegisterViewModel();

        var result = await _controller.Register(model);

        var viewResult = result as ViewResult;
        Assert.That(model, Is.EqualTo(viewResult.Model));
    }

    [Test]
    public async Task RegisterPost_RedirectsToHome_WhenSuccessfullyRegistered()
    {
        var model = new RegisterViewModel()
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "Password2@"
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<Users>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.Register(model);

        var redirect = result as RedirectToActionResult;

        Assert.That(redirect.ActionName, Is.EqualTo("Index"));
        Assert.That(redirect.ControllerName, Is.EqualTo("Home"));
    }

    [Test]
    public async Task RegisterPost_AddsErrors_WhenIdentityFails()
    {
        var model = new RegisterViewModel
        {
            Username = "Test",
            Email = "email@test.com",
        };

        var identityError = new IdentityError
        {
            Description = "Password is too simple."
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<Users>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        var result = await _controller.Register(model);

        Assert.That(_controller.ModelState.IsValid, Is.False);
        Assert.That(_controller.ModelState[string.Empty].Errors[0].ErrorMessage, Is.EqualTo("Password is too simple."));
    }
}
