using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using InfrastructureApp.Services;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Services
{
    [TestFixture]
    public class GeocodingServiceTests
    {
        // -----------------------------
        // Fake HttpMessageHandler
        // -----------------------------
        // Lets us intercept the outgoing request URL and return a controlled response body,
        // without ever touching the real internet / Google API.
        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public Uri? LastRequestUri { get; private set; }

            public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequestUri = request.RequestUri;
                return Task.FromResult(_responder(request));
            }
        }

        //Missing API key -> throw InvalidOperationException
        [Test]
        public void GeocodeAsync_ThrowsInvalidOperation_WhenApiKeyMissing()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["GoogleMaps:GeocodingApiKey"].Returns((string?)null); // missing key

            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient().Returns(new HttpClient(new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)))); // won't be called because key missing

            var service = new GeocodingService(factory, config);

            // Act + Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.GeocodeAsync("Salem Oregon"));

            Assert.That(ex!.Message, Is.EqualTo("Geocoding key not configured."));
        }

        //Google returns status OK -> parse JSON and return (lat, lng)
        [Test]
        public async Task GeocodeAsync_ReturnsLatLng_WhenGoogleStatusOk()
        {
            // Arrange
            var json = """
            {
              "status": "OK",
              "results": [
                { "geometry": { "location": { "lat": 44.9429, "lng": -123.0351 } } }
              ]
            }
            """;

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handler);

            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient().Returns(httpClient);

            var config = Substitute.For<IConfiguration>();
            config["GoogleMaps:GeocodingApiKey"].Returns("FAKE_KEY");

            var service = new GeocodingService(factory, config);

            // Act
            var (lat, lng) = await service.GeocodeAsync("Salem Oregon");

            // Assert
            Assert.That(lat, Is.EqualTo(44.9429).Within(0.000001));
            Assert.That(lng, Is.EqualTo(-123.0351).Within(0.000001));
        }

        //URL building is correct (address, region, key)
        [Test]
        public async Task GeocodeAsync_BuildsExpectedRequestUrl_WithEncodedAddress_RegionAndKey()
        {
            var json = """
            {
            "status": "OK",
            "results": [
                { "geometry": { "location": { "lat": 1.0, "lng": 2.0 } } }
            ]
            }
            """;

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handler);

            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient().Returns(httpClient);

            var config = Substitute.For<IConfiguration>();
            config["GoogleMaps:GeocodingApiKey"].Returns("MY KEY");

            var service = new GeocodingService(factory, config);

            await service.GeocodeAsync("Salem Oregon");

            var uri = handler.LastRequestUri!;
            Assert.That(uri.Host, Is.EqualTo("maps.googleapis.com"));
            Assert.That(uri.AbsolutePath, Is.EqualTo("/maps/api/geocode/json"));

            //  Parse query robustly
            var query = uri.Query; // "?address=Salem%20Oregon&region=us&key=MY%20KEY"
            Assert.That(query, Does.Contain("region=us"));

            //  Assert the decoded *values* (most reliable)
            var parsed = System.Web.HttpUtility.ParseQueryString(query);
            Assert.That(parsed["address"], Is.EqualTo("Salem Oregon"));
            Assert.That(parsed["region"], Is.EqualTo("us"));
            Assert.That(parsed["key"], Is.EqualTo("MY KEY"));

            //  Optional: if you still want to assert encoding specifically:
            Assert.That(uri.OriginalString, Does.Contain("address=Salem%20Oregon"));
            Assert.That(uri.OriginalString, Does.Contain("key=MY%20KEY"));
        }

        //Google status not OK + error_message present -> throw with details
        [Test]
        public void GeocodeAsync_ThrowsException_WhenGoogleStatusNotOk_AndIncludesStatusAndMessage()
        {
            // Arrange
            var json = """
            {
              "status": "REQUEST_DENIED",
              "error_message": "The provided API key is invalid."
            }
            """;

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handler);

            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient().Returns(httpClient);

            var config = Substitute.For<IConfiguration>();
            config["GoogleMaps:GeocodingApiKey"].Returns("FAKE_KEY");

            var service = new GeocodingService(factory, config);

            // Act + Assert
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await service.GeocodeAsync("Salem Oregon"));

            Assert.That(ex!.Message, Does.Contain("Google Geocode failed: REQUEST_DENIED"));
            Assert.That(ex.Message, Does.Contain("The provided API key is invalid."));
        }

        //Google status not OK + error_message missing -> throw with fallback text
        [Test]
        public void GeocodeAsync_ThrowsException_WhenGoogleStatusNotOk_AndErrorMessageMissing()
        {
            // Arrange
            var json = """
            {
              "status": "ZERO_RESULTS"
            }
            """;

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handler);

            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient().Returns(httpClient);

            var config = Substitute.For<IConfiguration>();
            config["GoogleMaps:GeocodingApiKey"].Returns("FAKE_KEY");

            var service = new GeocodingService(factory, config);

            // Act + Assert
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await service.GeocodeAsync("some nonsense address"));

            Assert.That(ex!.Message, Does.Contain("Google Geocode failed: ZERO_RESULTS"));
            Assert.That(ex.Message, Does.Contain("Unknown geocoding error."));
        }
    }
}