using NUnit.Framework;
using System.IO;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class LayoutLanguageSelectorTests
    {
        /// <summary>
        /// Verifies the Google Translate language selector markup exists in the shared layout.
        /// This checks selector presence only (not translation behavior).
        /// </summary>
        [Test]
        public void Layout_ShouldContain_GoogleTranslateSelector()
        {
            // Arrange
            var layoutPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..",
                "InfrastructureApp",
                "Views",
                "Shared",
                "_Layout.cshtml"
            );

            // Act
            var layoutContent = File.ReadAllText(layoutPath);

            // Assert
            Assert.That(layoutContent, Does.Contain("google_translate_element"));
        }
    }
}