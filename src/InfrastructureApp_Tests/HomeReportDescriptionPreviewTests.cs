using InfrastructureApp.Models;
using NUnit.Framework;

namespace InfrastructureApp_Tests;

[TestFixture]
public class HomeReportDescriptionPreviewTests
{
    // -------------------------------------------------------
    // TEST 1: Short descriptions should remain unchanged
    // -------------------------------------------------------
    [Test]
    public void BuildDescriptionPreview_ShortDescription_ReturnsUnchanged()
    {
        var description = "Small pothole on Main Street";

        var preview = ReportIssue.BuildDescriptionPreview(description, previewLength: 60);

        Assert.That(preview, Is.EqualTo(description));
    }

    // -------------------------------------------------------
    // TEST 2: Description exactly at limit should not change
    // -------------------------------------------------------
    [Test]
    public void BuildDescriptionPreview_DescriptionAtLimit_ReturnsUnchanged()
    {
        var description = new string('a', 60);

        var preview = ReportIssue.BuildDescriptionPreview(description, previewLength: 60);

        Assert.That(preview, Is.EqualTo(description));
    }

    // -------------------------------------------------------
    // TEST 3: Long descriptions should be truncated with "..."
    // -------------------------------------------------------
    [Test]
    public void BuildDescriptionPreview_LongDescription_TruncatesWithEllipsis()
    {
        var description = new string('a', 61);

        var preview = ReportIssue.BuildDescriptionPreview(description, previewLength: 60);

        Assert.That(preview, Is.EqualTo(new string('a', 60) + "..."));
    }

    // -------------------------------------------------------
    // TEST 4: Null description should return empty string
    // -------------------------------------------------------
    [Test]
    public void BuildDescriptionPreview_NullDescription_ReturnsEmptyString()
    {
        var preview = ReportIssue.BuildDescriptionPreview(null, previewLength: 60);

        Assert.That(preview, Is.EqualTo(string.Empty));
    }

    // -------------------------------------------------------
    // TEST 5: Whitespace description should return empty string
    // -------------------------------------------------------
    [Test]
    public void BuildDescriptionPreview_WhitespaceDescription_ReturnsEmptyString()
    {
        var preview = ReportIssue.BuildDescriptionPreview("   ", previewLength: 60);

        Assert.That(preview, Is.EqualTo(string.Empty));
    }
}
