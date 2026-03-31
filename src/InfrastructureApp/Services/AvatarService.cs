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

        public async Task<(bool Success, string? ErrorMessage)> SaveUploadedAvatarAsync(Users user, IFormFile file)
        {
            const long maxBytes = 5 * 1024 * 1024;
            var allowedTypes = new[] { "image/jpeg", "image/png" };

            if (file == null || file.Length == 0)
                return (false, "Please select an image file to upload.");
            if (!allowedTypes.Contains(file.ContentType))
                return (false, "Only JPG and PNG files are accepted.");
            if (file.Length > maxBytes)
                return (false, "File exceeds the 5 MB size limit.");


            var ext = Path.GetExtension(file.FileName);  
            var fileName = $"{Guid.NewGuid()}{ext}";
            var savePath = Path.Combine("wwwroot", "uploads", "avatars", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

            await using var stream = System.IO.File.Create(savePath);
            await file.CopyToAsync(stream);   

            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            await _userManager.UpdateAsync(user);
            return (true, null);      
        }
    }
}