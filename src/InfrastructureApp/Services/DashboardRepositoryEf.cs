using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InfrastructureApp.Services
{
    public class DashboardRepositoryEf : IDashboardRepository
    {
        
        private readonly ApplicationDbContext _db; // Database context used for queries
        private readonly UserManager<Users> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardRepositoryEf(ApplicationDbContext db, UserManager<Users> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }


        // Attempt to load a seeded/demo user from the database.
        // If none exists, return placeholder values so the dashboard
        // remains functional until full account integration is enabled.
       public async Task<DashboardViewModel> GetDashboardSummaryAsync()
        {
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext!.User);

            if (currentUser == null)
                return BuildPlaceholderDashboard();

            // Reload fresh from DB to ensure AvatarKey/AvatarUrl are populated
            var freshUser = await _userManager.FindByIdAsync(currentUser.Id);

            if (freshUser == null)
                return BuildPlaceholderDashboard();

            var dashboard = await BuildDashboardForUserAsync(freshUser);
            dashboard.IsOwnDashboard = true;
            return dashboard;
        }

        // Returns public profile data for a user looked up by username
        public async Task<DashboardViewModel?> GetPublicProfileAsync(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return null;

            var reportsSubmitted = await _db.ReportIssue
                .AsNoTracking()
                .CountAsync(r => r.UserId == user.Id);

            var pointsRow = await _db.UserPoints
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var selectedBackground = await GetSelectedDashboardBackgroundAsync(user.Id, user.SelectedDashboardBackgroundKey);

            return new DashboardViewModel
            {
                Username         = user.UserName ?? username,
                Email            = "",   // not exposed on public profile
                ReportsSubmitted = reportsSubmitted,
                Points           = pointsRow?.CurrentPoints ?? 0,
                AvatarKey        = user.AvatarKey,
                AvatarUrl        = user.AvatarUrl,
                SelectedDashboardBackgroundKey = selectedBackground?.Key,
                PersonalInfoBackgroundUrl = selectedBackground?.ImageUrl
            };
        }

        // Returns default dashboard values when no user is found
        private static DashboardViewModel BuildPlaceholderDashboard()
        {
            return new DashboardViewModel
            {
                Username = "DemoUser",
                Email = "demo@example.com",
                ReportsSubmitted = 0,
                Points = 0
            };
        }

        // Builds dashboard values using database data
        private async Task<DashboardViewModel> BuildDashboardForUserAsync(Users user)
        {
            // Count how many reports this user submitted
            var reportsSubmitted = await _db.ReportIssue
                .AsNoTracking()
                .CountAsync(r => r.UserId == user.Id);

            // Get user points (if they exist)
            var pointsRow = await _db.UserPoints
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var unlockedBackgroundNames = await GetUnlockedDashboardBackgroundNamesAsync(user.Id);
            var selectedBackground = ResolveSelectedBackground(unlockedBackgroundNames, user.SelectedDashboardBackgroundKey);

            return new DashboardViewModel
            {
                Username = user.UserName ?? "DemoUser",
                Email = user.Email ?? "demo@example.com",
                ReportsSubmitted = reportsSubmitted,
                Points = pointsRow?.CurrentPoints ?? 0,
                AvatarKey = user.AvatarKey,
                AvatarUrl = user.AvatarUrl,
                SelectedDashboardBackgroundKey = selectedBackground?.Key,
                PersonalInfoBackgroundUrl = selectedBackground?.ImageUrl,
                AvailableDashboardBackgrounds = PointsShopCatalog.BuildDashboardBackgroundOptions(unlockedBackgroundNames)
            };
        }

        public async Task<bool> UpdateSelectedDashboardBackgroundAsync(string userId, string? selectedBackgroundKey)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(selectedBackgroundKey))
            {
                user.SelectedDashboardBackgroundKey = null;
                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }

            var selectedBackground = PointsShopCatalog.GetDashboardBackgroundByKey(selectedBackgroundKey);
            if (selectedBackground == null)
            {
                return false;
            }

            var unlockedBackgroundNames = await GetUnlockedDashboardBackgroundNamesAsync(userId);
            if (!unlockedBackgroundNames.Contains(selectedBackground.Name))
            {
                return false;
            }

            user.SelectedDashboardBackgroundKey = selectedBackground.Key;
            var updateResult = await _userManager.UpdateAsync(user);
            return updateResult.Succeeded;
        }

        private async Task<List<string>> GetUnlockedDashboardBackgroundNamesAsync(string userId)
        {
            var validBackgroundNames = PointsShopCatalog.DashboardBackgrounds
                .Select(background => background.Name)
                .ToList();

            return await _db.UserShopItemPurchases
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Join(
                    _db.ShopItems.AsNoTracking(),
                    purchase => purchase.ShopItemId,
                    item => item.Id,
                    (purchase, item) => item.Name)
                .Where(name => validBackgroundNames.Contains(name))
                .Distinct()
                .ToListAsync();
        }

        private async Task<DashboardBackgroundDefinition?> GetSelectedDashboardBackgroundAsync(string userId, string? selectedBackgroundKey)
        {
            var unlockedBackgroundNames = await GetUnlockedDashboardBackgroundNamesAsync(userId);
            return ResolveSelectedBackground(unlockedBackgroundNames, selectedBackgroundKey);
        }

        private static DashboardBackgroundDefinition? ResolveSelectedBackground(IEnumerable<string> unlockedBackgroundNames, string? selectedBackgroundKey)
        {
            var selectedBackground = PointsShopCatalog.GetDashboardBackgroundByKey(selectedBackgroundKey);
            if (selectedBackground == null)
            {
                return null;
            }

            return unlockedBackgroundNames.Contains(selectedBackground.Name)
                ? selectedBackground
                : null;
        }
    }
}
