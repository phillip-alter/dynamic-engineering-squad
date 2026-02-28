//this tests whether the controller calls the service, the controller builds navigation URL's correctly. We are verifying that the controller asks asp.net to generate the correct url.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Controllers.Api;
using InfrastructureApp.Dtos;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Routing;
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

            _urlHelper.Action(Arg.Any<UrlActionContext>())
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


        //service call verification test:
        [Test]
        public async Task GetNearby_CallsService_WithExpectedParameters()
        {
            // Arrange
            var svc = Substitute.For<INearbyIssueService>();
            svc.GetNearbyIssuesAsync(44.84, -123.23, 5)
            .Returns(new List<NearbyIssueDTO>());

            var controller = new ReportsApiController(svc);

            // Act
            _ = await controller.GetNearby(44.84, -123.23, 5);

            // Assert
            await svc.Received(1).GetNearbyIssuesAsync(44.84, -123.23, 5);
        }

        //pass through payload test
        [Test]
        public async Task GetNearby_ReturnsOk_WithServicePayload_IncludingDetailsUrl()
        {
            // Arrange
            var svc = Substitute.For<INearbyIssueService>();

            var expected = new List<NearbyIssueDTO>
            {
                new NearbyIssueDTO
                {
                    Id = 1,
                    Status = "Approved",
                    CreatedAt = new DateTime(2026, 2, 24, 12, 0, 0, DateTimeKind.Utc),
                    Latitude = 44.84,
                    Longitude = -123.23,
                    DistanceMiles = 0.01,
                    DetailsUrl = "/ReportIssue/Details/1"
                }
            };

            svc.GetNearbyIssuesAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>())
            .Returns(expected);

            var controller = new ReportsApiController(svc);

            // Act
            var result = await controller.GetNearby(44.84, -123.23, 5);

            // Assert
            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var payload = ok!.Value as IEnumerable<NearbyIssueDTO>;
            Assert.That(payload, Is.Not.Null);

            var dto = payload!.Single();
            Assert.That(dto.DetailsUrl, Is.EqualTo("/ReportIssue/Details/1"));
        }
    }
}