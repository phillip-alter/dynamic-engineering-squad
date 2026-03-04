using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using InfrastructureApp.ViewModels;
using NUnit.Framework;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace InfrastructureApp_Tests.ViewModels;

[TestFixture]
public class ReportIssueViewModelTests
{
    //Creates a list to hold validation errors, checks the object against its DataAnnotations.
    //it validates every property, not just “Required” on the object. Returns the list of validation results (empty list = passes).
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    //tests whether a description was given
    [Test]
    public void Description_IsRequired()
    {
        var vm = new ReportIssueViewModel
        {
            Description = "",            // invalid
            Photo = null,                // invalid too, but we assert the description error exists
            Latitude = 0,
            Longitude = 0
        };

        var results = Validate(vm);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("Please enter a description.")));
    }

    //tests description length
    [Test]
    public void Description_TooLong_Fails()
    {
        var vm = new ReportIssueViewModel
        {
            Description = new string('a', 301), // limit set to 300 characters
            Photo = null,
            Latitude = 0,
            Longitude = 0
        };

        var results = Validate(vm);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("300 characters")));
    }

    //tests latitude range
    [Test]
    public void Latitude_OutOfRange_Fails()
    {
        var vm = new ReportIssueViewModel
        {
            Description = "Valid",
            Photo = null,
            Latitude = 91,     // invalid
            Longitude = 0
        };

        var results = Validate(vm);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("Latitude")));
    }

    //tests longitude range
    [Test]
    public void Longitude_OutOfRange_Fails()
    {
        var vm = new ReportIssueViewModel
        {
            Description = "Valid",
            Photo = null,
            Latitude = 0,
            Longitude = 181     // invalid
        };

        var results = Validate(vm);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("Longitude")));
    }

    //if geolocation is not provided, test fails
    [Test]
    public void Location_IsRequired_WhenMissing_Fails()
    {
        var vm = new ReportIssueViewModel
        {
            Description = "Valid",
            Photo = null,
            Latitude = null,
            Longitude = null
        };

        var results = Validate(vm);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssueViewModel.Latitude)) ||
            r.MemberNames.Contains(nameof(ReportIssueViewModel.Longitude))));
    }

    //if geolocation is provided, test passes
    [Test]
    public void Location_WhenProvided_Passes()
    {
        var vm = new ReportIssueViewModel
        {
            Description = "Valid",
            Photo = null,
            Latitude = 44.9m,
            Longitude = -123.0m
        };

        var results = Validate(vm);

        Assert.That(results, Has.None.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssueViewModel.Latitude)) ||
            r.MemberNames.Contains(nameof(ReportIssueViewModel.Longitude))));
    }

    //tests whether a photo was not uploaded
    [Test]
    public void Photo_IsRequired()
    {
        var vm = new ReportIssueViewModel
        {
            Description = "Valid",
            Photo = null,      // invalid
            Latitude = 0,
            Longitude = 0
        };

        var results = Validate(vm);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssueViewModel.Photo)) &&
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("Please upload a photo of the damage.")));
    }

    //test whether a photo was provided
    [Test]
    public void Photo_WhenProvided_Passes()
    {
        //build fake uploaded file to test
        var bytes = new byte[] { 1, 2, 3 };
        using var stream = new MemoryStream(bytes);

        IFormFile file = new FormFile(stream, 0, bytes.Length, "Photo", "test.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        //tests that fake file
        var vm = new ReportIssueViewModel
        {
            Description = "Valid",
            Photo = file,      // valid
            Latitude = 0,
            Longitude = 0
        };

        var results = Validate(vm);

        Assert.That(results, Has.None.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssueViewModel.Photo))));
    }
}
