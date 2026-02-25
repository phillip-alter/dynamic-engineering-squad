using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Controllers.Api;
using InfrastructureApp.Dtos;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Controllers.Api
{
    [TestFixture]
    public class ReportsApiControllerTests
    {
        // Mocked dependency: controller should not talk to DB or do business logic.
        // The service is responsible for actually finding nearby issues.
        private INearbyIssueService _nearbyIssueService = null!;

        // The controller under test
        private ReportsApiController _controller = null!;

        // Mock Url helper because the controller calls Url.Action(...)
        // In unit tests there is no real routing pipeline, so Url would be null unless we set it.
        private IUrlHelper _urlHelper = null!;

        [SetUp]
        public void SetUp()
        {
            _nearbyIssueService = Substitute.For<INearbyIssueService>();
            _controller = new ReportsApiController(_nearbyIssueService);

            _urlHelper = Substitute.For<IUrlHelper>();
            _controller.Url = _urlHelper;
        }

        [Test]
        public async Task GetNearby_ReturnsOk_WithProjectedFields_AndDetailsUrl()
        {
            // ---------------------------------------------------------
            // WHAT we are testing:
            // - Controller returns HTTP 200 OK
            // - It maps (projects) NearbyIssueDTO objects into API objects
            //   that include DetailsUrl built via Url.Action(...)
            //
            // WHY:
            // - The controller’s main responsibility is "response shaping".
            // - The frontend map/list depends on these fields existing.
            // - If someone accidentally removes/renames a field, this test catches it.
            // ---------------------------------------------------------

            // Arrange (inputs to the controller)
            double lat = 44.8;
            double lng = -123.2;
            double radius = 5;

            var created = new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc);

            // Fake service results (what the controller will project)
            var serviceResults = new List<NearbyIssueDTO>
            {
                new NearbyIssueDTO
                {
                    Id = 1,
                    Status = "Approved",
                    CreatedAt = created,
                    Latitude = 44.801,
                    Longitude = -123.201,
                    DistanceMiles = 1.25 // nullable double? in DTO
                }
            };

            // Mock service call
            _nearbyIssueService.GetNearbyIssuesAsync(lat, lng, radius)
                .Returns(serviceResults);

            // Mock Url.Action(...) so DetailsUrl becomes predictable
            _urlHelper.Action(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>())
                .Returns(callInfo =>
                {
                    // callInfo.ArgAt<object>(2) = the route values object (anonymous: new { id = r.Id })
                    var valuesObj = callInfo.ArgAt<object>(2);

                    var idProp = valuesObj.GetType().GetProperty("id");
                    var id = (int)idProp!.GetValue(valuesObj)!;

                    return $"/ReportIssue/Details/{id}";
                });

            // Act
            var actionResult = await _controller.GetNearby(lat, lng, radius);

            // Assert (status code)
            Assert.That(actionResult.Result, Is.TypeOf<OkObjectResult>());
            var ok = (OkObjectResult)actionResult.Result!;
            Assert.That(ok.Value, Is.Not.Null);

            // ok.Value is an IEnumerable of anonymous objects, because controller uses Select(new { ... })
            var items = ((IEnumerable<object>)ok.Value!).ToList();
            Assert.That(items.Count, Is.EqualTo(1));

            var item = items[0];

            // Read anonymous object's properties via reflection
            var idProp = item.GetType().GetProperty("Id");
            var statusProp = item.GetType().GetProperty("Status");
            var createdProp = item.GetType().GetProperty("CreatedAt");
            var latProp = item.GetType().GetProperty("Latitude");
            var lngProp = item.GetType().GetProperty("Longitude");
            var distProp = item.GetType().GetProperty("DistanceMiles");
            var urlProp = item.GetType().GetProperty("DetailsUrl");

            Assert.That(idProp, Is.Not.Null);
            Assert.That(statusProp, Is.Not.Null);
            Assert.That(createdProp, Is.Not.Null);
            Assert.That(latProp, Is.Not.Null);
            Assert.That(lngProp, Is.Not.Null);
            Assert.That(distProp, Is.Not.Null);
            Assert.That(urlProp, Is.Not.Null);

            Assert.That((int)idProp!.GetValue(item)!, Is.EqualTo(1));
            Assert.That((string)statusProp!.GetValue(item)!, Is.EqualTo("Approved"));
            Assert.That((DateTime)createdProp!.GetValue(item)!, Is.EqualTo(created));
            Assert.That((double)latProp!.GetValue(item)!, Is.EqualTo(44.801));
            Assert.That((double)lngProp!.GetValue(item)!, Is.EqualTo(-123.201));

            // DistanceMiles is nullable (double?) so cast to double?
            Assert.That((double?)distProp!.GetValue(item)!, Is.EqualTo(1.25));

            Assert.That((string)urlProp!.GetValue(item)!, Is.EqualTo("/ReportIssue/Details/1"));

            // Assert (controller/service wiring)
            await _nearbyIssueService.Received(1).GetNearbyIssuesAsync(lat, lng, radius);
        }

        [Test]
        public async Task GetNearby_WhenDistanceMilesIsNull_ReturnsOk_AndDistanceMilesIsNullInResponse()
        {
            // ---------------------------------------------------------
            // WHAT we are testing:
            // - NearbyIssueDTO.DistanceMiles is double? (nullable)
            // - If the service returns null, the controller should still return OK
            //   and include null in the API response
            //
            // WHY:
            // - Null values happen in real systems (missing data, edge cases).
            // - This prevents accidental NullReference/casting issues in projection.
            // ---------------------------------------------------------

            // Arrange
            var serviceResults = new List<NearbyIssueDTO>
            {
                new NearbyIssueDTO
                {
                    Id = 2,
                    Status = "Approved",
                    CreatedAt = new DateTime(2026, 2, 21, 12, 0, 0, DateTimeKind.Utc),
                    Latitude = 44.9,
                    Longitude = -123.3,
                    DistanceMiles = null
                }
            };

            _nearbyIssueService.GetNearbyIssuesAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>())
                .Returns(serviceResults);

            _urlHelper.Action(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>())
                .Returns("/ReportIssue/Details/2");

            // Act
            var actionResult = await _controller.GetNearby(44.8, -123.2, 5);

            // Assert
            Assert.That(actionResult.Result, Is.TypeOf<OkObjectResult>());
            var ok = (OkObjectResult)actionResult.Result!;
            Assert.That(ok.Value, Is.Not.Null);

            var items = ((IEnumerable<object>)ok.Value!).ToList();
            Assert.That(items.Count, Is.EqualTo(1));

            var item = items[0];
            var distProp = item.GetType().GetProperty("DistanceMiles");
            Assert.That(distProp, Is.Not.Null);

            // DistanceMiles should be null in response
            Assert.That((double?)distProp!.GetValue(item), Is.Null);
        }

        [Test]
        public async Task GetNearby_WhenServiceReturnsEmpty_ReturnsOk_WithEmptyCollection()
        {
            // ---------------------------------------------------------
            // WHAT we are testing:
            // - No nearby issues -> return OK with an empty collection
            //
            // WHY:
            // - Frontend map expects "an array" even when no results.
            // - "No results" is not an error condition.
            // ---------------------------------------------------------

            // Arrange
            _nearbyIssueService.GetNearbyIssuesAsync(44.8, -123.2, 5)
                .Returns(new List<NearbyIssueDTO>());

            // Act
            var actionResult = await _controller.GetNearby(44.8, -123.2, 5);

            // Assert
            Assert.That(actionResult.Result, Is.TypeOf<OkObjectResult>());
            var ok = (OkObjectResult)actionResult.Result!;
            Assert.That(ok.Value, Is.Not.Null);

            var items = ((IEnumerable<object>)ok.Value!).ToList();
            Assert.That(items.Count, Is.EqualTo(0));

            await _nearbyIssueService.Received(1).GetNearbyIssuesAsync(44.8, -123.2, 5);
        }

        [Test]
        public async Task GetNearby_UsesDefaultRadiusOf5_WhenNotProvided()
        {
            // ---------------------------------------------------------
            // WHAT we are testing:
            // - Controller method has radiusMiles = 5 default parameter.
            // - If caller omits radiusMiles, it should call service with 5.
            //
            // WHY:
            // - Your JS might omit radiusMiles sometimes.
            // - This test catches regressions if someone changes the default.
            // ---------------------------------------------------------

            // Arrange
            double lat = 44.8;
            double lng = -123.2;
            double defaultRadius = 5;

            _nearbyIssueService.GetNearbyIssuesAsync(lat, lng, defaultRadius)
                .Returns(new List<NearbyIssueDTO>());

            // Act (call without radiusMiles)
            var actionResult = await _controller.GetNearby(lat, lng);

            // Assert
            Assert.That(actionResult.Result, Is.TypeOf<OkObjectResult>());
            await _nearbyIssueService.Received(1).GetNearbyIssuesAsync(lat, lng, defaultRadius);
        }

        [Test]
        public async Task GetNearby_CallsUrlAction_WithExpectedActionControllerAndId()
        {
            // WHAT: Controller builds DetailsUrl using Url.Action("Details","ReportIssue", new { id = r.Id })
            // WHY: Your map UI depends on this link. If action/controller/id changes, navigation breaks.

            // Arrange
            var serviceResults = new List<NearbyIssueDTO>
            {
                new NearbyIssueDTO
                {
                    Id = 99,
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow,
                    Latitude = 1,
                    Longitude = 2,
                    DistanceMiles = 0.5
                }
            };

            _nearbyIssueService
                .GetNearbyIssuesAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>())
                .Returns(serviceResults);

            // Capture the anonymous route-values object passed to Url.Action(...)
            object? capturedRouteValues = null;

            // IMPORTANT: Stub the SAME overload the controller uses (string,string,object)
            _urlHelper
                .Action("Details", "ReportIssue", Arg.Do<object?>(o => capturedRouteValues = o))
                .Returns("/ReportIssue/Details/99");

            // Act
            var actionResult = await _controller.GetNearby(44.8, -123.2, 5);

            // Assert (sanity)
            Assert.That(actionResult.Result, Is.TypeOf<OkObjectResult>());

            // Assert 1: Url.Action called exactly once with the expected action/controller
            _urlHelper.Received(1).Action("Details", "ReportIssue", Arg.Any<object>());

            // Assert 2: Route values included id = 99
            Assert.That(capturedRouteValues, Is.Not.Null);

            // Controller passes an anonymous object: new { id = r.Id }
            // So we read the "id" property using reflection.
            var idProp = capturedRouteValues!.GetType().GetProperty("id");
            Assert.That(idProp, Is.Not.Null);

            var idValue = (int)idProp!.GetValue(capturedRouteValues)!;
            Assert.That(idValue, Is.EqualTo(99));
        }
    }
}