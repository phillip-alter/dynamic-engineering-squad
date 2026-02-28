using System;
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

    public ContentModerationService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<ModerationResult> CheckAsync(string text, CancellationToken ct = default)
    {
        // Empty descriptions are fine (or you can require them via validation)
        if (string.IsNullOrWhiteSpace(text))
            return new ModerationResult(IsAllowed: true, Flagged: false);

        var apiKey = _config["OpenAI:APIkey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Missing OpenAI:APIkey configuration.");

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/moderations");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // Using the recommended moderation model name
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

        using var resp = await _http.SendAsync(req, ct);

        // “Fail closed”: if moderation cannot be performed, don’t allow submission
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Moderation request failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        // Parse minimal fields:
        // results[0].flagged and results[0].categories
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
                    category = prop.Name; // e.g. "hate", "harassment/threatening"
                    break;
                }
            }
        }

        return new ModerationResult(IsAllowed: false, Flagged: true, ReasonCategory: category);
    }
}