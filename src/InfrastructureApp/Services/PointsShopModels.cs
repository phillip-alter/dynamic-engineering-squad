using System;
using System.Collections.Generic;

namespace InfrastructureApp.Services
{
    public class PointsShopItemSummary
    {
        public int Id { get; init; }

        public string Name { get; init; } = "";

        public string Description { get; init; } = "";

        public int CostPoints { get; init; }

        public bool IsSinglePurchase { get; init; }

        public bool IsOwned { get; init; }

        public bool CanPurchase { get; init; }
    }

    public class PointsShopSnapshot
    {
        public int CurrentPoints { get; init; }

        public IReadOnlyList<PointsShopItemSummary> Items { get; init; } = Array.Empty<PointsShopItemSummary>();
    }

    public class PointsShopPurchaseResult
    {
        public bool Succeeded { get; init; }

        public string Message { get; init; } = "";

        public int RemainingPoints { get; init; }

        public int? ShopItemId { get; init; }

        public static PointsShopPurchaseResult Success(string message, int remainingPoints, int shopItemId)
            => new PointsShopPurchaseResult
            {
                Succeeded = true,
                Message = message,
                RemainingPoints = remainingPoints,
                ShopItemId = shopItemId
            };

        public static PointsShopPurchaseResult Failure(string message, int remainingPoints, int? shopItemId = null)
            => new PointsShopPurchaseResult
            {
                Succeeded = false,
                Message = message,
                RemainingPoints = remainingPoints,
                ShopItemId = shopItemId
            };
    }
}
