using NUnit.Framework;
using System;
using InfrastructureApp.Services.ContentModeration;

namespace InfrastructureApp_Tests.Services.ContentModeration
{
    [TestFixture]
    public class ContentModerationRejectedExceptionTests
    {
        [Test]
        public void Ctor_SetsMessage()
        {
            // Arrange
            var message = "Rejected by moderation";

            // Act
            var ex = new ContentModerationRejectedException(message);

            // Assert
            Assert.That(ex.Message, Is.EqualTo(message));
        }

        [Test]
        public void Ctor_WhenCategoryProvided_SetsCategory()
        {
            // Arrange
            var message = "Rejected by moderation";
            var category = "hate";

            // Act
            var ex = new ContentModerationRejectedException(message, category);

            // Assert
            Assert.That(ex.Category, Is.EqualTo(category));
        }

        [Test]
        public void Ctor_WhenCategoryNotProvided_CategoryIsNull()
        {
            // Arrange
            var message = "Rejected by moderation";

            // Act
            var ex = new ContentModerationRejectedException(message);

            // Assert
            Assert.That(ex.Category, Is.Null);
        }

        [Test]
        public void ThrowAndCatch_AsException_PreservesData()
        {
            // Arrange
            var message = "Rejected by moderation";
            var category = "violence";

            try
            {
                // Act
                throw new ContentModerationRejectedException(message, category);
            }
            catch (Exception e)
            {
                // Assert
                Assert.That(e, Is.TypeOf<ContentModerationRejectedException>());

                var typed = (ContentModerationRejectedException)e;
                Assert.That(typed.Message, Is.EqualTo(message));
                Assert.That(typed.Category, Is.EqualTo(category));
            }
        }

        [Test]
        public void Ctor_DoesNotSetInnerException()
        {
            // Arrange
            var message = "Rejected by moderation";

            // Act
            var ex = new ContentModerationRejectedException(message, "self-harm");

            // Assert
            Assert.That(ex.InnerException, Is.Null);
        }
    }
}