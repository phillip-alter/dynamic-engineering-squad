using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InfrastructureApp_Tests;

[TestFixture]
public class AccountControllerTests
{
    private Mock<UserManager<Users>> _mockUserManager;
    private AccountController _controller;

    [SetUp]
    public void Setup()
    {
        var store = new Mock<IUserStore<Users>>();
        _mockUserManager = new Mock<UserManager<Users>>(store.Object, null, null, null, null, null, null, null, null);
        _controller = new AccountController(_mockUserManager.Object);
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


}
