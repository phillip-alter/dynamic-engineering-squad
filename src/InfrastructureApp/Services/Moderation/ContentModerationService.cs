using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace InfrastructureApp.Services.Moderation;

public sealed class ContentModerationService : IContentModerationService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    // Simple retry schedule: 2 retries (total 3 attempts)
    private static readonly int[] RetryDelaysMs = { 1000, 3000, 8000 };

    public ContentModerationService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<ModerationResult> CheckAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ModerationResult(IsAllowed: true, Flagged: false);

        var apiKey = _config["OpenAIModerationAPIkey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Missing OpenAIModerationAPIkey configuration.");

        // Add an upper bound so the request doesn't hang forever
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(40));

        for (int attempt = 0; attempt <= RetryDelaysMs.Length; attempt++)
        {
            Console.WriteLine($"[Moderation] Attempt #{attempt + 1}");

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/moderations");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model = "omni-moderation-latest",
                input = text
            };

            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            using var resp = await _http.SendAsync(req, timeoutCts.Token);

            Console.WriteLine($"[Moderation] Response Status: {(int)resp.StatusCode}");

            if (resp.IsSuccessStatusCode)
            {
                await using var stream = await resp.Content.ReadAsStreamAsync(timeoutCts.Token);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeoutCts.Token);

                var r0 = doc.RootElement.GetProperty("results")[0];
                bool flagged = r0.GetProperty("flagged").GetBoolean();

                if (!flagged)
                    return new ModerationResult(IsAllowed: true, Flagged: false);

                string? category = null;
                if (r0.TryGetProperty("categories", out var cats))
                {
                    foreach (var prop in cats.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.True)
                        {
                            category = prop.Name;
                            break;
                        }
                    }
                }

                return new ModerationResult(IsAllowed: false, Flagged: true, ReasonCategory: category);
            }

            // Retry only on 429 or transient 5xx
            var statusCode = (int)resp.StatusCode;
            bool shouldRetry = statusCode == 429 || statusCode >= 500;

            if (shouldRetry && attempt < RetryDelaysMs.Length)
            {
                // If server tells us how long to wait, respect it.
                var retryAfter = GetRetryAfterDelay(resp);
                var delay = retryAfter ?? TimeSpan.FromMilliseconds(RetryDelaysMs[attempt]);

                Console.WriteLine($"[Moderation] Retrying after {delay.TotalMilliseconds} ms");

                await Task.Delay(delay, timeoutCts.Token);
                continue;
            }

            // Fail with useful body
            var body = await resp.Content.ReadAsStringAsync(timeoutCts.Token);
            throw new HttpRequestException(
                $"Moderation request failed: {statusCode} {resp.ReasonPhrase}. Body: {body}"
            );
        }

        // Should never happen
        throw new InvalidOperationException("Unexpected moderation retry loop exit.");
    }

    private static TimeSpan? GetRetryAfterDelay(HttpResponseMessage resp)
    {
        // Retry-After can be seconds or a date; we handle seconds form
        if (resp.Headers.TryGetValues("Retry-After", out var values))
        {
            var v = values.FirstOrDefault();
            if (int.TryParse(v, out int seconds) && seconds >= 0)
                return TimeSpan.FromSeconds(seconds);
        }

        return null;
    }
}