//this tests whether the input validation path is correct. Whether q is null or empty, the happy path is correct, and the error path is correct

using System;
using System.Threading.Tasks;
using InfrastructureApp.Controllers.Api;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Controllers.Api
{
    [TestFixture]
    public class GeocodeApiControllerTests
    {
        // Fake version of the service dependency
        // (NEVER call Google APIs in unit tests)
        private IGeocodingService _geocodingService = null!;
        private GeocodeApiController _controller = null!;

        // Runs BEFORE every test
        [SetUp]
        public void SetUp()
        {
            // Create a mock object that behaves like IGeocodingService
            _geocodingService = Substitute.For<IGeocodingService>();
            _controller = new GeocodeApiController(_geocodingService);
        }

        // Invalid query parameter should return BadRequest
        // Runs same test multiple times with different inputs
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task Geocode_WhenQueryMissingOrWhitespace_ReturnsBadRequest_WithExpectedMessage(string? q)
        {
            // Act
            var result = await _controller.Geocode(q!);

            // Assert
            // Verify controller returned HTTP 400 BadRequest
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());

            var bad = (BadRequestObjectResult)result;
            Assert.That(bad.Value, Is.Not.Null);

            // Controller returns anonymous object:
            // new { message = "Missing query parameter 'q'." }
            // We must use reflection to read its property.
            var messageProp = bad.Value!.GetType().GetProperty("message");
            Assert.That(messageProp, Is.Not.Null);

            var messageValue = messageProp!.GetValue(bad.Value) as string;
            Assert.That(messageValue, Is.EqualTo("Missing query parameter 'q'."));

            // And service should NOT be called when input is invalid
            await _geocodingService.DidNotReceiveWithAnyArgs().GeocodeAsync(default!);
        }

        // Successful geocoding returns Ok(lat,lng)
        [Test]
        public async Task Geocode_WhenServiceSucceeds_ReturnsOk_WithLatLng()
        {
            // Arrange
            const string q = "123 Main St";

            // Fake coordinates returned by service
            const double lat = 44.0462;
            const double lng = -123.0220;

            // Configure mock service behavior:
            _geocodingService.GeocodeAsync(q).Returns(Task.FromResult((lat, lng)));

            // Act
            var result = await _controller.Geocode(q);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>());

            var ok = (OkObjectResult)result;
            Assert.That(ok.Value, Is.Not.Null);

            // Read object's lat/lng via reflection
            var latProp = ok.Value!.GetType().GetProperty("lat");
            var lngProp = ok.Value!.GetType().GetProperty("lng");

            Assert.That(latProp, Is.Not.Null);
            Assert.That(lngProp, Is.Not.Null);

            var latValue = (double)latProp!.GetValue(ok.Value)!;
            var lngValue = (double)lngProp!.GetValue(ok.Value)!;

            Assert.That(latValue, Is.EqualTo(lat));
            Assert.That(lngValue, Is.EqualTo(lng));

            await _geocodingService.Received(1).GeocodeAsync(q);
        }

        // Service throws exception → controller returns BadRequest
        [Test]
        public async Task Geocode_WhenServiceThrows_ReturnsBadRequest_WithExceptionMessage()
        {
            // Arrange
            const string q = "bad address";
            var ex = new Exception("Geocoding failed.");

            // Configure mock to THROW instead of return
            _geocodingService.GeocodeAsync(q).Returns<Task<(double lat, double lng)>>(x => throw ex);

            // Act
            var result = await _controller.Geocode(q);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());

            var bad = (BadRequestObjectResult)result;
            Assert.That(bad.Value, Is.Not.Null);

            var messageProp = bad.Value!.GetType().GetProperty("message");
            Assert.That(messageProp, Is.Not.Null);

            var messageValue = messageProp!.GetValue(bad.Value) as string;

            // Controller should forward exception message to client
            Assert.That(messageValue, Is.EqualTo("Geocoding failed."));

            await _geocodingService.Received(1).GeocodeAsync(q);
        }
    }
}