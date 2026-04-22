using System;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Dashboard
{
    [TestFixture]
    public class DashboardRepositoryEfTests
    {
        private ApplicationDbContext _db = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("DashboardRepositoryTest_" + Guid.NewGuid())
                .Options;

            _db = new ApplicationDbContext(options);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        [Test]
        public async Task GetPublicProfileAsync_WhenDashboardBackgroundPurchased_ReturnsBackgroundUrl()
        {
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "alice",
                Email = "alice@test.com",
                EmailConfirmed = true
            };
            _db.Users.Add(user);

            var backgroundItem = new ShopItem
            {
                Name = PointsShopCatalog.DashboardBackgroundImageItemName,
                Description = "Background unlock",
                CostPoints = 10,
                IsSinglePurchase = true,
                IsActive = true
            };
            _db.ShopItems.Add(backgroundItem);
            await _db.SaveChangesAsync();

            _db.UserShopItemPurchases.Add(new UserShopItemPurchase
            {
                UserId = user.Id,
                ShopItemId = backgroundItem.Id,
                CostPoints = backgroundItem.CostPoints,
                PurchasedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var repo = new DashboardRepositoryEf(_db, CreateUserManager(user), Mock.Of<IHttpContextAccessor>());

            var result = await repo.GetPublicProfileAsync("alice");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.PersonalInfoBackgroundUrl, Is.EqualTo(PointsShopCatalog.DashboardBackgroundImageUrl));
            Assert.That(result.HasPersonalInfoBackground, Is.True);
        }

        [Test]
        public async Task GetPublicProfileAsync_WhenDashboardBackgroundNotPurchased_ReturnsNoBackgroundUrl()
        {
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "bob",
                Email = "bob@test.com",
                EmailConfirmed = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var repo = new DashboardRepositoryEf(_db, CreateUserManager(user), Mock.Of<IHttpContextAccessor>());

            var result = await repo.GetPublicProfileAsync("bob");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.PersonalInfoBackgroundUrl, Is.Null);
            Assert.That(result.HasPersonalInfoBackground, Is.False);
        }

        private static UserManager<Users> CreateUserManager(Users user)
        {
            var store = new Mock<IUserStore<Users>>();
            var userManager = new Mock<UserManager<Users>>(
                store.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManager.Setup(m => m.FindByNameAsync(user.UserName!))
                .ReturnsAsync(user);

            return userManager.Object;
        }
    }
}
