using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.Account;
using Microsoft.AspNetCore.Identity;

namespace InfrastructureApp.Services
{
    public class AvatarService : IAvatarService
    
    {
        private readonly UserManager<Users> _userManager;

        public AvatarService(UserManager<Users> userManager)
        {
            _userManager = userManager;
        }

        public ChooseAvatarViewModel BuildChooseAvatarViewModel(Users user, string? selectedKey = null, string? error = null)
        {
            var current = selectedKey ?? user.AvatarKey;

            return new ChooseAvatarViewModel
            {
                SelectedAvatarKey = current,
                ErrorMessage = error,
                Options = AvatarCatalog.Keys.Select(k => new AvatarOptionViewModel
                {
                    Key = k,
                    Url = AvatarCatalog.ToUrl(k),
                    IsSelected = string.Equals(k, current, StringComparison.OrdinalIgnoreCase)
                }).ToList()
            };
        }

        public async Task<(bool Success, string? ErrorMessage)> SaveAvatarAsync(Users user, string? selectedAvatarKey)
        {
            if (!AvatarCatalog.IsValid(selectedAvatarKey))
                return (false, "Please select an avatar.");

            user.AvatarKey = selectedAvatarKey;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return (false, "Could not save your avatar. Please try again.");

            return (true, null);
        }
    }
}