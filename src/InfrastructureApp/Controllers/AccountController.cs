using InfrastructureApp.ViewModels.Account;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace InfrastructureApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<Users> userManager;

        public AccountController(UserManager<Users> userManager)
        {
            this.userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new Users
            {
                UserName = model.Username,
                NormalizedUserName = model.Username,
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper(),
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return RedirectToAction("Index","Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}
