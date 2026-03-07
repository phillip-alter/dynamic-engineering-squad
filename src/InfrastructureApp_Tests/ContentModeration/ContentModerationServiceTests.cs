using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InfrastructureApp.Services.ContentModeration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Services.ContentModeration
{
    [TestFixture]
    public class ContentModerationServiceTests
    {
        private string _tempRoot = null!;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "ContentModerationServiceTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }

        // -----------------------------
        // Fake HttpMessageHandler
        // -----------------------------
        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public int CallCount { get; private set; }
            public HttpRequestMessage? LastRequest { get; private set; }

            public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                LastRequest = request;
                return Task.FromResult(_responder(request));
            }
        }

        private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        private IHostEnvironment BuildHostEnvironment(string contentRootPath)
        {
            var env = Substitute.For<IHostEnvironment>();
            env.ContentRootPath.Returns(contentRootPath);
            return env;
        }

        private static HttpClient BuildHttpClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler);
        }

        private string CreateBadWordsFile(params string[] words)
        {
            var dataDir = Path.Combine(_tempRoot, "Data", "Moderation");
            Directory.CreateDirectory(dataDir);

            var filePath = Path.Combine(dataDir, "badWords.txt");
            File.WriteAllLines(filePath, words);

            return filePath;
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object body)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json")
            };
        }

        [Test]
        public async Task CheckAsync_WhenTextIsNullOrWhitespace_ReturnsAllowed()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called for empty input."));

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>());
            var env = BuildHostEnvironment(_tempRoot);

            var sut = new ContentModerationService(http, config, env);

            var result1 = await sut.CheckAsync("");
            var result2 = await sut.CheckAsync("   ");
            var result3 = await sut.CheckAsync((string?)null!);

            Assert.Multiple(() =>
            {
                Assert.That(result1.Performed, Is.True);
                Assert.That(result1.IsAllowed, Is.True);
                Assert.That(result1.Flagged, Is.False);

                Assert.That(result2.Performed, Is.True);
                Assert.That(result2.IsAllowed, Is.True);
                Assert.That(result2.Flagged, Is.False);

                Assert.That(result3.Performed, Is.True);
                Assert.That(result3.IsAllowed, Is.True);
                Assert.That(result3.Flagged, Is.False);

                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task CheckAsync_WhenLocalBadWordExists_BlocksImmediately_AndDoesNotCallHttp()
        {
            CreateBadWordsFile("shit", "damn");

            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called when local blocklist catches input."));

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = "test-key"
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            var result = await sut.CheckAsync("This pothole is shit.");

            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.True);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Does.Contain("Blocked word detected"));
                Assert.That(result.Reason, Does.Contain("shit"));
                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task CheckAsync_LocalBlocklist_IsCaseInsensitive()
        {
            CreateBadWordsFile("shit");

            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called when local blocklist catches input."));

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = "test-key"
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            var result = await sut.CheckAsync("This road is SHIT");

            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.True);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Does.Contain("shit").IgnoreCase);
                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task CheckAsync_LocalBlocklist_CatchesSimpleObfuscation()
        {
            CreateBadWordsFile("shit");

            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called when local blocklist catches input."));

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = "test-key"
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            var result = await sut.CheckAsync("This road is sh1t");

            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.True);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Does.Contain("shit"));
                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task CheckAsync_WhenBadWordsFileMissing_AndApiKeyMissing_ReturnsNotPerformed()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called when API key is missing."));

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "does-not-exist.txt")
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            var result = await sut.CheckAsync("Normal description with no blocked words.");

            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.False);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Is.EqualTo("Missing moderation API key."));
                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task CheckAsync_WhenLocalListDoesNotMatch_AndOpenAiAllows_ReturnsAllowed()
        {
            CreateBadWordsFile("shit", "damn");

            var handler = new FakeHttpMessageHandler(req =>
            {
                return JsonResponse(HttpStatusCode.OK, new
                {
                    results = new[]
                    {
                        new
                        {
                            flagged = false,
                            categories = new { }
                        }
                    }
                });
            });

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = "test-key"
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            var result = await sut.CheckAsync("There is a pothole near Main Street.");

            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.True);
                Assert.That(result.IsAllowed, Is.True);
                Assert.That(result.Flagged, Is.False);
                Assert.That(handler.CallCount, Is.EqualTo(1));
                Assert.That(handler.LastRequest, Is.Not.Null);
                Assert.That(handler.LastRequest!.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(handler.LastRequest.RequestUri!.ToString(), Is.EqualTo("https://api.openai.com/v1/moderations"));
                Assert.That(handler.LastRequest.Headers.Authorization, Is.Not.Null);
                Assert.That(handler.LastRequest.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
                Assert.That(handler.LastRequest.Headers.Authorization!.Parameter, Is.EqualTo("test-key"));
            });
        }

        [Test]
        public async Task CheckAsync_WhenLocalListDoesNotMatch_AndOpenAiFlags_ReturnsBlockedWithCategory()
        {
            CreateBadWordsFile("shit", "damn");

            var handler = new FakeHttpMessageHandler(req =>
            {
                return JsonResponse(HttpStatusCode.OK, new
                {
                    results = new[]
                    {
                        new
                        {
                            flagged = true,
                            categories = new
                            {
                                harassment = true,
                                violence = false
                            }
                        }
                    }
                });
            });

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = "test-key"
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            var result = await sut.CheckAsync("I want to insult somebody without using a listed bad word.");

            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.True);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Is.EqualTo("Flagged category: harassment"));
                Assert.That(handler.CallCount, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task CheckAsync_WhenApiKeyMissing_AndNoLocalMatch_ReturnsMissingKeyResult()
        {
            CreateBadWordsFile("shit", "damn");

            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called when API key is missing."));

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = ""
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            var result = await sut.CheckAsync("There is a pothole near Main Street.");

            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.False);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Is.EqualTo("Missing moderation API key."));
                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }
    }
}