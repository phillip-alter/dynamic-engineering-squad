using System.Collections.Generic;
using InfrastructureApp.Models;

namespace InfrastructureApp.Services
{
    public static class PointsShopCatalog
    {
        public const string DashboardBackgroundImageItemName = "Dashboard Background Image";
        public const string DashboardBackgroundImageUrl = "/Images/dashboard-personal-info-bg.svg";

        public static IReadOnlyList<ShopItem> GetStarterItems()
        {
            return new[]
            {
                new ShopItem
                {
                    Name = DashboardBackgroundImageItemName,
                    Description = "Unlock a dashboard background image for the personal information card.",
                    CostPoints = 10,
                    IsSinglePurchase = true,
                    IsActive = true
                },
                new ShopItem
                {
                    Name = "Golden Reporter Title",
                    Description = "Unlock a special title that highlights your contribution streak.",
                    CostPoints = 25,
                    IsSinglePurchase = true,
                    IsActive = true
                },
                new ShopItem
                {
                    Name = "Map Pin Cosmetic",
                    Description = "Claim a cosmetic map pin reward for your profile collection.",
                    CostPoints = 40,
                    IsSinglePurchase = true,
                    IsActive = true
                },
                new ShopItem
                {
                    Name = "Premium Profile Frame",
                    Description = "Add a premium-looking frame to your user profile presentation.",
                    CostPoints = 60,
                    IsSinglePurchase = true,
                    IsActive = true
                },
                new ShopItem
                {
                    Name = "Dark Theme Badge",
                    Description = "Unlock a cosmetic badge that shows off your shop progress.",
                    CostPoints = 80,
                    IsSinglePurchase = true,
                    IsActive = true
                }
            };
        }
    }
}
