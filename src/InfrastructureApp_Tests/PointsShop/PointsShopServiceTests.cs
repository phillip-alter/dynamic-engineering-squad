using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;

namespace InfrastructureApp_Tests.PointsShop
{
    [TestFixture]
    public class PointsShopServiceTests
    {
        private ApplicationDbContext _db = null!;
        private PointsShopService _service = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("PointsShopServiceTest_" + Guid.NewGuid())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _db = new ApplicationDbContext(options);
            _service = new PointsShopService(_db);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        private async Task<ShopItem> AddShopItemAsync(
            string name,
            int costPoints,
            bool isSinglePurchase = true,
            bool isActive = true)
        {
            var item = new ShopItem
            {
                Name = name,
                Description = $"{name} description",
                CostPoints = costPoints,
                IsSinglePurchase = isSinglePurchase,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };

            _db.ShopItems.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        private async Task SeedPointsAsync(string userId, int currentPoints, int lifetimePoints = 0)
        {
            _db.UserPoints.Add(new UserPoints
            {
                UserId = userId,
                CurrentPoints = currentPoints,
                LifetimePoints = lifetimePoints == 0 ? currentPoints : lifetimePoints,
                LastUpdated = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        [Test]
        public async Task PurchaseAsync_SuccessfulPurchase_DeductsCorrectPoints()
        {
            var userId = "user-1";
            var item = await AddShopItemAsync("Golden Reporter Title", 25);
            await SeedPointsAsync(userId, 60, 60);

            var result = await _service.PurchaseAsync(userId, item.Id);

            Assert.That(result.Succeeded, Is.True);

            var points = await _db.UserPoints.SingleAsync(p => p.UserId == userId);
            Assert.That(points.CurrentPoints, Is.EqualTo(35));
            Assert.That(points.LifetimePoints, Is.EqualTo(60));
        }

        [Test]
        public async Task PurchaseAsync_SuccessfulPurchase_RecordsUserPurchase()
        {
            var userId = "user-2";
            var item = await AddShopItemAsync("Map Pin Cosmetic", 40);
            await SeedPointsAsync(userId, 75, 75);

            var result = await _service.PurchaseAsync(userId, item.Id);

            Assert.That(result.Succeeded, Is.True);

            var purchase = await _db.UserShopItemPurchases.SingleAsync(p => p.UserId == userId && p.ShopItemId == item.Id);
            Assert.That(purchase.CostPoints, Is.EqualTo(40));
        }

        [Test]
        public async Task PurchaseAsync_InsufficientPoints_RejectsPurchase()
        {
            var userId = "user-3";
            var item = await AddShopItemAsync("Premium Profile Frame", 60);
            await SeedPointsAsync(userId, 20, 20);

            var result = await _service.PurchaseAsync(userId, item.Id);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain("enough points"));
            Assert.That(await _db.UserShopItemPurchases.AnyAsync(), Is.False);

            var points = await _db.UserPoints.SingleAsync(p => p.UserId == userId);
            Assert.That(points.CurrentPoints, Is.EqualTo(20));
        }

        [Test]
        public async Task PurchaseAsync_SinglePurchaseItemCannotBeBoughtTwice()
        {
            var userId = "user-4";
            var item = await AddShopItemAsync("Dark Theme Badge", 15);
            await SeedPointsAsync(userId, 100, 100);

            var first = await _service.PurchaseAsync(userId, item.Id);
            var second = await _service.PurchaseAsync(userId, item.Id);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.False);
            Assert.That(second.Message, Does.Contain("already own"));

            var purchases = await _db.UserShopItemPurchases
                .Where(p => p.UserId == userId && p.ShopItemId == item.Id)
                .ToListAsync();

            Assert.That(purchases.Count, Is.EqualTo(1));

            var points = await _db.UserPoints.SingleAsync(p => p.UserId == userId);
            Assert.That(points.CurrentPoints, Is.EqualTo(85));
        }

        [Test]
        public async Task PurchaseAsync_NonexistentOrInactiveItem_RejectsPurchase()
        {
            var userId = "user-5";
            var inactiveItem = await AddShopItemAsync("Inactive Cosmetic", 30, isActive: false);
            await SeedPointsAsync(userId, 100, 100);

            var missingResult = await _service.PurchaseAsync(userId, 9999);
            var inactiveResult = await _service.PurchaseAsync(userId, inactiveItem.Id);

            Assert.That(missingResult.Succeeded, Is.False);
            Assert.That(inactiveResult.Succeeded, Is.False);
            Assert.That(await _db.UserShopItemPurchases.AnyAsync(), Is.False);
        }

        [Test]
        public async Task GetShopAsync_ReturnsBalanceOwnershipAndPurchaseStatus()
        {
            var userId = "user-6";
            var ownedItem = await AddShopItemAsync("Owned Cosmetic", 20);
            var affordableItem = await AddShopItemAsync("Affordable Cosmetic", 10);
            await AddShopItemAsync("Inactive Cosmetic", 5, isActive: false);
            await SeedPointsAsync(userId, 15, 50);

            _db.UserShopItemPurchases.Add(new UserShopItemPurchase
            {
                UserId = userId,
                ShopItemId = ownedItem.Id,
                CostPoints = ownedItem.CostPoints,
                PurchasedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var snapshot = await _service.GetShopAsync(userId);

            Assert.That(snapshot.CurrentPoints, Is.EqualTo(15));
            Assert.That(snapshot.Items.Count, Is.EqualTo(2));

            var ownedSummary = snapshot.Items.Single(i => i.Id == ownedItem.Id);
            Assert.That(ownedSummary.IsOwned, Is.True);
            Assert.That(ownedSummary.CanPurchase, Is.False);

            var affordableSummary = snapshot.Items.Single(i => i.Id == affordableItem.Id);
            Assert.That(affordableSummary.IsOwned, Is.False);
            Assert.That(affordableSummary.CanPurchase, Is.True);
        }

        [Test]
        public async Task GetShopAsync_WhenCatalogAlreadyExists_AddsMissingStarterBackgroundItem()
        {
            var userId = "user-7";
            await AddShopItemAsync("Legacy Cosmetic", 99);
            await SeedPointsAsync(userId, 50, 50);

            var snapshot = await _service.GetShopAsync(userId);

            Assert.That(snapshot.Items.Any(i => i.Name == PointsShopCatalog.DashboardBackgroundImageItemName), Is.True);
            Assert.That(snapshot.Items.Count(i => PointsShopCatalog.GetDashboardBackgroundByName(i.Name) != null), Is.EqualTo(6));
        }
    }
}
