using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InfrastructureApp_Tests.Services
{
    [TestFixture]
    public class AvatarServiceTests
    {
        private Mock<UserManager<Users>> _mockUserManager = null!;
        private AvatarService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _mockUserManager = MockUserManager();
            _service = new AvatarService(_mockUserManager.Object);
        }

        // -------------------------------
        //   Helper: Fake UserManager
        // -------------------------------
        private static Mock<UserManager<Users>> MockUserManager()
        {
            var store = new Mock<IUserStore<Users>>();
            return new Mock<UserManager<Users>>(
                store.Object, null, null, null, null, null, null, null, null
            );
        }

        // -------------------------------
        //   TEST: Building the ViewModel
        // -------------------------------
      [Test]
      public void BuildChooseAvatarViewModel_ShouldBuildWithCorrectSelection()
        {
            // Arrange: use a real key
            var firstKey = AvatarCatalog.Keys.First();
            var user = new Users { AvatarKey = firstKey };

            // Act
            var vm = _service.BuildChooseAvatarViewModel(user);

            // Assert
            Assert.That(vm.SelectedAvatarKey, Is.EqualTo(firstKey));
            Assert.That(vm.Options.Any(o => o.Key == firstKey), Is.True);
            Assert.That(vm.Options.Single(o => o.Key == firstKey).IsSelected, Is.True);
        }

        [Test]
        public void BuildChooseAvatarViewModel_ShouldOverrideSelection_WhenSelectedKeyProvided()
        {
            // Arrange: use a real key
            var firstKey = AvatarCatalog.Keys.First();
            var user = new Users { AvatarKey = firstKey };
            // Act
            var vm = _service.BuildChooseAvatarViewModel(user);

            // Assert
            Assert.That(vm.SelectedAvatarKey, Is.EqualTo(firstKey));
            Assert.That(vm.Options.Any(o => o.Key == firstKey), Is.True);
            Assert.That(vm.Options.Single(o => o.Key == firstKey).IsSelected, Is.True);
        }

        [Test]
        public void BuildChooseAvatarViewModel_ShouldIncludeErrorMessage()
        {
            // Arrange
            var user = new Users { AvatarKey = "avatar1" };

            // Act
            var vm = _service.BuildChooseAvatarViewModel(user, error: "Something went wrong");

            // Assert
            Assert.That(vm.ErrorMessage, Is.EqualTo("Something went wrong"));
        }

        // -------------------------------
        //   TEST: SaveAvatarAsync
        // -------------------------------
        [Test]
        public async Task SaveAvatarAsync_ShouldFail_WhenAvatarKeyIsInvalid()
        {
            // Arrange
            var user = new Users { AvatarKey = "avatar1" };
            string invalidKey = null; // AvatarCatalog.IsValid(null) should be false

            // Act
            var (success, error) = await _service.SaveAvatarAsync(user, invalidKey);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Please select an avatar."));
        }

        [Test]
        public async Task SaveAvatarAsync_ShouldSave_WhenValidAvatar()
        {
            // Arrange
            var user = new Users { AvatarKey = "avatar1" };
            var newKey = AvatarCatalog.Keys.First();

            _mockUserManager
                .Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var (success, error) = await _service.SaveAvatarAsync(user, newKey);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(user.AvatarKey, Is.EqualTo(newKey));
        }

        [Test]
        public async Task SaveAvatarAsync_ShouldFail_WhenUserManagerFails()
        {
            // Arrange
            var user = new Users { AvatarKey = "avatar1" };
            var newKey = AvatarCatalog.Keys.First();

            _mockUserManager
                .Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed());

            // Act
            var (success, error) = await _service.SaveAvatarAsync(user, newKey);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Could not save your avatar. Please try again."));
        }
    }
}