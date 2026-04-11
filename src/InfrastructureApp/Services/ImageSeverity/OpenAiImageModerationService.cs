//implements image moderation through OpenAI's moderation service

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace InfrastructureApp.Services.ImageSeverity
{
    public sealed class OpenAiImageModerationService : IImageModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OpenAiImageModerationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<ImageModerationResult> ModerateImageAsync(string imageUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return ImageModerationResult.Failed("Image URL was empty.");

            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:ModerationModel"] ?? "omni-moderation-latest";

            if (string.IsNullOrWhiteSpace(apiKey))
                return ImageModerationResult.Failed("OpenAI API key is missing.");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/moderations");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model,
                input = new object[]
                {
                    new
                    {
                        type = "image_url",
                        image_url = new
                        {
                            url = imageUrl
                        }
                    }
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            try
            {
                using var response = await _httpClient.SendAsync(request, ct);
                var json = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                    return ImageModerationResult.Failed($"Moderation API returned {(int)response.StatusCode}.");

                using var doc = JsonDocument.Parse(json);

                var results = doc.RootElement.GetProperty("results");
                if (results.GetArrayLength() == 0)
                    return ImageModerationResult.Failed("Moderation API returned no results.");

                var first = results[0];

                var flagged = first.TryGetProperty("flagged", out var flaggedProp) && flaggedProp.GetBoolean();

                if (flagged)
                {
                    return ImageModerationResult.Rejected("Image was flagged by moderation.");
                }

                return ImageModerationResult.Passed();
            }
            catch (Exception ex)
            {
                return ImageModerationResult.Failed(ex.Message);
            }
        }
    }
}