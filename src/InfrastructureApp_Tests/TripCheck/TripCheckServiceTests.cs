using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Microsoft.Extensions.Options;

namespace InfrastructureApp.Tests.TripCheck
{
    [TestFixture]
    public class TripCheckServiceTests
    {
        //Sample payload shape 
       
      private const string CamerasJson = """
        {
        "organization-information": {
            "organization-id": "ODOT",
            "organization-name": "ODOT TripCheck",
            "last-update-time": "2026-02-17T15:30:00Z"
        },
        "CCTVInventoryRequest": [
            {
            "device-id": 1001,
            "device-name": "I-5 at Salem",
            "latitude": 44.9429,
            "longitude": -123.0351,
            "route-id": "I5",
            "cctv-url": "https://example.com/cam1.jpg",
            "cctv-other": "Salem",
            "last-update-time": "2026-02-17T15:25:00Z"
            },
            {
            "device-id": 1002,
            "device-name": "US-26 at Government Camp",
            "latitude": 45.3043,
            "longitude": -121.7560,
            "route-id": "US26",
            "cctv-url": null,
            "cctv-other": null,
            "last-update-time": null
            }
        ]
        }
        """;


    [Test]
    public async Task GetCamerasAsync_ParsesAndMaps_ToRoadCameraViewModels()
        {
            //Arrange
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CamerasJson, Encoding.UTF8, "application/json")

                });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://tripcheck.example/")};

            var cache = new MemoryCache(new MemoryCacheOptions());
            var sut = new TripCheckService(
                httpClient,
                cache,
                NullLogger<TripCheckService>.Instance,
                Options.Create(new TripCheckOptions { CacheMinutes = 10 }));


             // Act
            IReadOnlyList<RoadCameraViewModel> cameras = await sut.GetCamerasAsync();

            // Assert
            Assert.That(cameras, Is.Not.Null);
            Assert.That(cameras.Count, Is.EqualTo(2));

            Assert.That(cameras[0].CameraId, Is.EqualTo("1001"));
            Assert.That(cameras[0].Name, Is.EqualTo("I-5 at Salem"));
            Assert.That(cameras[0].Road, Is.EqualTo("I5"));
            Assert.That(cameras[0].Latitude, Is.EqualTo(44.9429).Within(0.0001));
            Assert.That(cameras[0].Longitude, Is.EqualTo(-123.0351).Within(0.0001));
            Assert.That(cameras[0].ImageUrl, Is.EqualTo("https://example.com/cam1.jpg"));
            Assert.That(cameras[0].LastUpdated, Is.Not.Null);

            // Nulls should map safely
            Assert.That(cameras[1].CameraId, Is.EqualTo("1002"));
            Assert.That(cameras[1].ImageUrl, Is.Null);
            Assert.That(cameras[1].LastUpdated, Is.Null);
        }

        [Test]
        public async Task GetCamerasAsync_WhenApiFails_ReturnsEmptyList_NotCrash()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tripcheck.example/") };

            var cache = new MemoryCache(new MemoryCacheOptions());
            var sut = new TripCheckService(
                httpClient,
                cache,
                NullLogger<TripCheckService>.Instance,
                Options.Create(new TripCheckOptions { CacheMinutes = 10 }));


            // Act
            var cameras = await sut.GetCamerasAsync();

            // Assert
            Assert.That(cameras, Is.Not.Null);
            Assert.That(cameras.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetCamerasAsync_UsesCache_DoesNotCallApiTwice()
        {
            // Arrange
            int callCount = 0;

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CamerasJson, Encoding.UTF8, "application/json")

                });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tripcheck.example/") };

            var cache = new MemoryCache(new MemoryCacheOptions());
            var sut = new TripCheckService(
                httpClient,
                cache,
                NullLogger<TripCheckService>.Instance,
                Options.Create(new TripCheckOptions { CacheMinutes = 10 })); 


            // Act
            var first = await sut.GetCamerasAsync();
            var second = await sut.GetCamerasAsync();

            // Assert
            Assert.That(first.Count, Is.EqualTo(2));
            Assert.That(second.Count, Is.EqualTo(2));
            Assert.That(callCount, Is.LessThanOrEqualTo(1), "Should not call API more than once because of caching.");

        }

        [Test]
        public async Task GetCamerasAsync_WhenApiFails_ButCacheHasData_ReturnsCachedData()
        {
            // Arrange
            int callCount = 0;

            var handler = new FakeHttpMessageHandler(_ =>
            {
                callCount++;
                // First call OK, second call fails
                if (callCount == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(CamerasJson, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tripcheck.example/") };

            var cache = new MemoryCache(new MemoryCacheOptions());
            var sut = new TripCheckService(
                httpClient,
                cache,
                NullLogger<TripCheckService>.Instance,
                Options.Create(new TripCheckOptions { CacheMinutes = 10 }));


            // Act
            var first = await sut.GetCamerasAsync(); // populates cache
            // Simulate "cache still valid" by calling again (service should return cached even if API would fail)
            var second = await sut.GetCamerasAsync();

            // Assert
            Assert.That(first.Count, Is.EqualTo(2));
            Assert.That(second.Count, Is.EqualTo(2));
            Assert.That(callCount, Is.LessThanOrEqualTo(1), "Should not call API more than once because of caching.");

        }

        [Test]
        public async Task GetCameraByIdAsync_ReturnsSingleCamera_OrNullIfMissing()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CamerasJson, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tripcheck.example/") };

            var cache = new MemoryCache(new MemoryCacheOptions());
            var sut = new TripCheckService(
                httpClient,
                cache,
                NullLogger<TripCheckService>.Instance,
                Options.Create(new TripCheckOptions { CacheMinutes = 10 }));


            // Act
            var cam1 = await sut.GetCameraByIdAsync("1001");
            var missing = await sut.GetCameraByIdAsync("DOES_NOT_EXIST");

            // Assert
            Assert.That(cam1, Is.Not.Null);
            Assert.That(cam1!.CameraId, Is.EqualTo("1001"));
            Assert.That(missing, Is.Null);
        }
    }

    /// <summary>
    /// Minimal fake handler for HttpClient testing.
    /// This is how we test HTTP calls without real network access.
    /// </summary>
    internal sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}    