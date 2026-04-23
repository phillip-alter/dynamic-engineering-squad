namespace InfrastructureApp.ViewModels
{
    public class PointsShopItemViewModel
    {
        public int Id { get; init; }

        public string Name { get; init; } = "";

        public string Description { get; init; } = "";

        public int CostPoints { get; init; }

        public bool IsSinglePurchase { get; init; }

        public bool IsOwned { get; init; }

        public bool CanPurchase { get; init; }
    }
}
