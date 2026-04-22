using System;
using System.Collections.Generic;
using System.Linq;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels;

namespace InfrastructureApp.Services
{
    public sealed class DashboardBackgroundDefinition
    {
        public required string Key { get; init; }

        public required string Name { get; init; }

        public required string Description { get; init; }

        public required string ImageUrl { get; init; }

        public int CostPoints { get; init; } = 10;
    }

    public static class PointsShopCatalog
    {
        public const string DashboardBackgroundImageItemName = "Dashboard Background Image";

        public static readonly IReadOnlyList<DashboardBackgroundDefinition> DashboardBackgrounds =
            new[]
            {
                new DashboardBackgroundDefinition
                {
                    Key = "safety-wave",
                    Name = DashboardBackgroundImageItemName,
                    Description = "Unlock a dashboard background image for the personal information card.",
                    ImageUrl = "/Images/dashboard-personal-info-bg.svg"
                },
                new DashboardBackgroundDefinition
                {
                    Key = "city-grid",
                    Name = "City Grid Background",
                    Description = "Unlock a city grid backdrop for your dashboard card.",
                    ImageUrl = "/Images/dashboard-city-grid-bg.svg"
                },
                new DashboardBackgroundDefinition
                {
                    Key = "safety-stripe",
                    Name = "Safety Stripe Background",
                    Description = "Unlock bold safety stripes behind your personal information card.",
                    ImageUrl = "/Images/dashboard-safety-stripe-bg.svg"
                },
                new DashboardBackgroundDefinition
                {
                    Key = "blueprint",
                    Name = "Blueprint Background",
                    Description = "Unlock a blueprint-inspired dashboard background.",
                    ImageUrl = "/Images/dashboard-blueprint-bg.svg"
                },
                new DashboardBackgroundDefinition
                {
                    Key = "aurora-night",
                    Name = "Aurora Night Background",
                    Description = "Unlock a glowing night-sky dashboard background.",
                    ImageUrl = "/Images/dashboard-aurora-night-bg.svg"
                },
                new DashboardBackgroundDefinition
                {
                    Key = "signal-glow",
                    Name = "Signal Glow Background",
                    Description = "Unlock a signal-inspired dashboard background with warm highlights.",
                    ImageUrl = "/Images/dashboard-signal-glow-bg.svg"
                }
            };

        public static IReadOnlyList<ShopItem> GetStarterItems()
        {
            var backgroundItems = DashboardBackgrounds
                .Select(background => new ShopItem
                {
                    Name = background.Name,
                    Description = background.Description,
                    CostPoints = background.CostPoints,
                    IsSinglePurchase = true,
                    IsActive = true
                });

            var otherItems = new[]
            {
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

            return backgroundItems.Concat(otherItems).ToList().AsReadOnly();
        }

        public static DashboardBackgroundDefinition? GetDashboardBackgroundByKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return DashboardBackgrounds.FirstOrDefault(background => background.Key == key);
        }

        public static DashboardBackgroundDefinition? GetDashboardBackgroundByName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return DashboardBackgrounds.FirstOrDefault(background => background.Name == name);
        }

        public static IReadOnlyList<DashboardBackgroundOptionViewModel> BuildDashboardBackgroundOptions(IEnumerable<string> unlockedItemNames)
        {
            var unlockedNameSet = unlockedItemNames.ToHashSet(StringComparer.Ordinal);

            return DashboardBackgrounds
                .Where(background => unlockedNameSet.Contains(background.Name))
                .Select(background => new DashboardBackgroundOptionViewModel
                {
                    Key = background.Key,
                    Name = background.Name,
                    PreviewUrl = background.ImageUrl
                })
                .ToList()
                .AsReadOnly();
        }
    }
}
