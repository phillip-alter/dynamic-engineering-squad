/**This contains the behavior / logic:

load bad words

check local blocklist

call OpenAI moderation

return a decision**/


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace InfrastructureApp.Services.ContentModeration;

public sealed class ContentModerationService : IContentModerationService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;

    // Loaded once for the lifetime of the service
    private readonly Lazy<HashSet<string>> _badWords;

    // Simple retry schedule: 2 retries (total 3 attempts)
    private static readonly int[] RetryDelaysMs = { 1000, 3000, 8000 };

    // Pull out "word-like" tokens.
    // Keeps letters/numbers/apostrophes so we can normalize and compare.
    private static readonly Regex TokenRegex =
        new Regex(@"[a-z0-9']+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ContentModerationService(HttpClient http, IConfiguration config, IHostEnvironment env)
    {
        _http = http;
        _config = config;
        _env = env;
        _badWords = new Lazy<HashSet<string>>(LoadBadWords, isThreadSafe: true);
    }

    public async Task<ContentModerationResult> CheckAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ContentModerationResult(Performed: true, IsAllowed: true, Flagged: false);

        // -----------------------------------------
        // LAYER 1: Local blocklist check
        // -----------------------------------------
        var localMatch = FindBlockedWord(text);
        if (localMatch is not null)
        {
            Console.WriteLine($"[Moderation] Local blocklist hit: {localMatch}");

            return new ContentModerationResult(
                Performed: true,
                IsAllowed: false,
                Flagged: true,
                Reason: $"Blocked word detected: {localMatch}"
            );
        }

        // -----------------------------------------
        // LAYER 2: OpenAI moderation fallback
        // -----------------------------------------
        return await CheckWithOpenAiAsync(text, ct);
    }

    private string? FindBlockedWord(string input)
    {
        var set = _badWords.Value;

        // Normalize common obfuscation before tokenization
        var normalized = NormalizeForComparison(input);

        foreach (Match match in TokenRegex.Matches(normalized))
        {
            var token = match.Value;
            if (set.Contains(token))
                return token;
        }

        return null;
    }

    private HashSet<string> LoadBadWords()
    {
        // Put badWords.txt in your project root and copy it to output,
        // OR place it somewhere predictable like /Data/Moderation/badWords.txt
        var configuredPath = _config["Moderation:BadWordsFilePath"];

        string path;
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            path = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(_env.ContentRootPath, configuredPath);
        }
        else
        {
            path = Path.Combine(_env.ContentRootPath, "badWords.txt");
        }

        if (!File.Exists(path))
        {
            Console.WriteLine($"[Moderation] WARNING: bad words file not found at: {path}");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var words = File.ReadLines(path)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(NormalizeForComparison)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Console.WriteLine($"[Moderation] Loaded {words.Count} local blocked terms from {path}");
        return words;
    }

    private static string NormalizeForComparison(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var s = value.ToLowerInvariant();

        // Common leetspeak / obfuscation cleanup
        s = s.Replace('0', 'o')
             .Replace('1', 'i')
             .Replace('3', 'e')
             .Replace('4', 'a')
             .Replace('5', 's')
             .Replace('7', 't')
             .Replace('@', 'a')
             .Replace('$', 's')
             .Replace('!', 'i');

        // Remove punctuation between letters so:
        // f.u.c.k -> fuck
        // f-u-c-k -> fuck
        // f_u_c_k -> fuck
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c) || c == '\'' || char.IsWhiteSpace(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    private async Task<ContentModerationResult> CheckWithOpenAiAsync(string text, CancellationToken ct)
    {
        var apiKey = _config["OpenAIModerationAPIkey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ContentModerationResult(
                Performed: false,
                IsAllowed: false,
                Flagged: true,
                Reason: "Missing moderation API key."
            );
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(40));

        for (int attempt = 0; attempt <= RetryDelaysMs.Length; attempt++)
        {
            Console.WriteLine($"[Moderation] OpenAI attempt #{attempt + 1}");

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

            HttpResponseMessage? resp = null;
            try
            {
                resp = await _http.SendAsync(req, timeoutCts.Token);
            }
            catch (Exception ex) when (attempt < RetryDelaysMs.Length)
            {
                var delay = TimeSpan.FromMilliseconds(RetryDelaysMs[attempt]);
                Console.WriteLine($"[Moderation] OpenAI exception: {ex.Message}. Retrying after {delay.TotalMilliseconds} ms");
                await Task.Delay(delay, timeoutCts.Token);
                continue;
            }
            catch (Exception ex)
            {
                return new ContentModerationResult(
                    Performed: false,
                    IsAllowed: false,
                    Flagged: true,
                    Reason: $"Moderation exception: {ex.Message}"
                );
            }

            using (resp)
            {
                Console.WriteLine($"[Moderation] OpenAI response status: {(int)resp.StatusCode}");

                if (resp.IsSuccessStatusCode)
                {
                    await using var stream = await resp.Content.ReadAsStreamAsync(timeoutCts.Token);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeoutCts.Token);

                    var r0 = doc.RootElement.GetProperty("results")[0];
                    bool flagged = r0.GetProperty("flagged").GetBoolean();

                    Console.WriteLine($"[Moderation] OpenAI flagged={flagged}");

                    if (!flagged)
                    {
                        Console.WriteLine("[Moderation] Decision: ALLOW");
                        return new ContentModerationResult(Performed: true, IsAllowed: true, Flagged: false);
                    }

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

                    Console.WriteLine($"[Moderation] Decision: BLOCK by OpenAI. category={category ?? "(none)"}");

                    return new ContentModerationResult(
                        Performed: true,
                        IsAllowed: false,
                        Flagged: true,
                        Reason: category is null ? "Flagged by moderation." : $"Flagged category: {category}"
                    );
                }

                var statusCode = (int)resp.StatusCode;
                bool shouldRetry = statusCode == 429 || statusCode >= 500;

                if (shouldRetry && attempt < RetryDelaysMs.Length)
                {
                    var retryAfter = GetRetryAfterDelay(resp);
                    var delay = retryAfter ?? TimeSpan.FromMilliseconds(RetryDelaysMs[attempt]);

                    Console.WriteLine($"[Moderation] Retrying after {delay.TotalMilliseconds} ms");
                    await Task.Delay(delay, timeoutCts.Token);
                    continue;
                }

                var body = await resp.Content.ReadAsStringAsync(timeoutCts.Token);
                return new ContentModerationResult(
                    Performed: false,
                    IsAllowed: false,
                    Flagged: true,
                    Reason: $"Moderation request failed: {statusCode} {resp.ReasonPhrase}. Body: {body}"
                );
            }
        }

        return new ContentModerationResult(
            Performed: false,
            IsAllowed: false,
            Flagged: true,
            Reason: "Unexpected moderation retry loop exit."
        );
    }

    private static TimeSpan? GetRetryAfterDelay(HttpResponseMessage resp)
    {
        if (resp.Headers.TryGetValues("Retry-After", out var values))
        {
            var v = values.FirstOrDefault();
            if (int.TryParse(v, out int seconds) && seconds >= 0)
                return TimeSpan.FromSeconds(seconds);
        }

        return null;
    }
}