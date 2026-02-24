using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.ViewComponents
{
    public class UserAvatarViewComponent : ViewComponent
    {
        private readonly UserManager<Users> _userManager;

        public UserAvatarViewComponent(UserManager<Users> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Content(string.Empty);

            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            var avatarUrl = AvatarCatalog.ToUrl(user?.AvatarKey);

            return View("Default", avatarUrl);
        }
    }
}