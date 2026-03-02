using NUnit.Framework;
using InfrastructureApp.Services.ContentModeration;

namespace InfrastructureApp_Tests.Services.ContentModeration
{
    [TestFixture]
    public class ContentModerationResultTests
    {
        [Test]
        public void Ctor_SetsAllProperties()
        {
            // Arrange + Act
            var result = new ContentModerationResult(
                Performed: true,
                IsAllowed: false,
                Flagged: true,
                Reason: "hate"
            );

            // Assert
            Assert.That(result.Performed, Is.True);
            Assert.That(result.IsAllowed, Is.False);
            Assert.That(result.Flagged, Is.True);
            Assert.That(result.Reason, Is.EqualTo("hate"));
        }

        [Test]
        public void Ctor_WhenReasonNotProvided_DefaultsToNull()
        {
            // Arrange + Act
            var result = new ContentModerationResult(
                Performed: true,
                IsAllowed: true,
                Flagged: false
                // Reason omitted
            );

            // Assert
            Assert.That(result.Reason, Is.Null);
        }

        [Test]
        public void Equality_TwoRecordsWithSameValues_AreEqual()
        {
            // Arrange
            var a = new ContentModerationResult(true, true, false, null);
            var b = new ContentModerationResult(true, true, false, null);

            // Assert (value-based equality for records)
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Equality_TwoRecordsWithDifferentValues_AreNotEqual()
        {
            // Arrange
            var a = new ContentModerationResult(true, true, false, null);
            var b = new ContentModerationResult(true, false, true, "violence");

            // Assert
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(a != b, Is.True);
        }

        [Test]
        public void WithExpression_CreatesNewInstanceWithModifiedValues()
        {
            // Arrange
            var original = new ContentModerationResult(
                Performed: true,
                IsAllowed: true,
                Flagged: false,
                Reason: null
            );

            // Act
            var updated = original with { IsAllowed = false, Flagged = true, Reason = "harassment" };

            // Assert: updated has new values
            Assert.That(updated.Performed, Is.True);
            Assert.That(updated.IsAllowed, Is.False);
            Assert.That(updated.Flagged, Is.True);
            Assert.That(updated.Reason, Is.EqualTo("harassment"));

            // Assert: original remains unchanged (immutability)
            Assert.That(original.IsAllowed, Is.True);
            Assert.That(original.Flagged, Is.False);
            Assert.That(original.Reason, Is.Null);

            // Assert: not same reference (records are reference types unless 'record struct')
            Assert.That(ReferenceEquals(original, updated), Is.False);
        }

        // Optional: ToString tests can be brittle if you rename properties or the compiler changes formatting,
        // but it can be useful for debugging expectations.
        [Test]
        public void ToString_IncludesTypeNameAndSomeFields()
        {
            // Arrange
            var result = new ContentModerationResult(true, false, true, "hate");

            // Act
            var text = result.ToString();

            // Assert (keep it loose so it doesn't break on minor formatting differences)
            Assert.That(text, Does.Contain(nameof(ContentModerationResult)));
            Assert.That(text, Does.Contain("Performed"));
            Assert.That(text, Does.Contain("IsAllowed"));
            Assert.That(text, Does.Contain("Flagged"));
            Assert.That(text, Does.Contain("Reason"));
        }
    }
}