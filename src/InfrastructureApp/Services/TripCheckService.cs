using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using InfrastructureApp.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfrastructureApp.Services
{
    public class TripCheckService : ITripCheckService
    {
        private const string CameraCacheKey = "TripCheck:Cameras";

        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TripCheckService> _logger;
        private readonly TripCheckOptions _options;

       public TripCheckService(
            HttpClient http,
            IMemoryCache cache,
            ILogger<TripCheckService> logger,
            IOptions<TripCheckOptions> options)
        {
            _http = http;
            _cache = cache;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<IReadOnlyList<RoadCameraViewModel>> GetCamerasAsync()
        {
            // 1) Cache first
            if (_cache.TryGetValue(CameraCacheKey, out List<RoadCameraViewModel>? cached) && cached is not null)
                return cached;

            try
            {
                // Ensure subscription key header is present for ODOT APIM
                if (!string.IsNullOrWhiteSpace(_options.SubscriptionKey))
                {
                    const string headerName = "Ocp-Apim-Subscription-Key";

                    // avoid duplicates if method is called multiple times
                    if (_http.DefaultRequestHeaders.Contains(headerName))
                        _http.DefaultRequestHeaders.Remove(headerName);

                    _http.DefaultRequestHeaders.Add(headerName, _options.SubscriptionKey);
                }
                else
                {
                    _logger.LogWarning("TripCheck SubscriptionKey is missing from configuration/user-secrets.");
                }

                var response = await _http.GetAsync("Cctv/Inventory");
                var json = await response.Content.ReadAsStringAsync();

                // Helpful logging so you can see WHY it's empty in the app
                _logger.LogWarning("TripCheck CCTV Inventory status: {StatusCode}", response.StatusCode);
                _logger.LogWarning("TripCheck response preview: {Preview}",
                    json.Length > 200 ? json.Substring(0, 200) : json);

                if (!response.IsSuccessStatusCode)
                {
                    // Don’t crash; return empty list and show friendly message in UI
                    return new List<RoadCameraViewModel>();
                }

                // TripCheck/ODOT returns a wrapper object with CCTVInventoryRequest array
                var inventory = JsonSerializer.Deserialize<CctvInventoryResponseDto>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var items = inventory?.CCTVInventoryRequest ?? new List<CctvInventoryItemDto>();
                var mapped = items.Select(MapInventoryItem).ToList();

                _cache.Set(CameraCacheKey, mapped, TimeSpan.FromMinutes(_options.CacheMinutes));
                return mapped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TripCheck camera fetch failed: {Message}", ex.Message);
                return new List<RoadCameraViewModel>();
            }
        }

        public async Task<RoadCameraViewModel?> GetCameraByIdAsync(string id)
        {
            var cameras = await GetCamerasAsync();
            return cameras.FirstOrDefault(c => c.CameraId == id);
        }

        private static RoadCameraViewModel MapInventoryItem(CctvInventoryItemDto dto)
        {
            DateTimeOffset? parsed = null;

            if (!string.IsNullOrWhiteSpace(dto.LastUpdateTime)
                && DateTimeOffset.TryParse(dto.LastUpdateTime, out var dt))
            {
                parsed = dt;
            }

            return new RoadCameraViewModel
            {
                CameraId = dto.DeviceId.ToString(),
                Name = dto.DeviceName,
                Road = dto.RouteId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                ImageUrl = dto.CctvUrl,
                LastUpdated = parsed
            };
        }

        private class CctvInventoryResponseDto
        {
            [JsonPropertyName("CCTVInventoryRequest")]
            public List<CctvInventoryItemDto>? CCTVInventoryRequest { get; set; }
        }

        private class CctvInventoryItemDto
        {
            [JsonPropertyName("device-id")]
            public int DeviceId { get; set; }

            [JsonPropertyName("device-name")]
            public string? DeviceName { get; set; }

            [JsonPropertyName("latitude")]
            public double Latitude { get; set; }

            [JsonPropertyName("longitude")]
            public double Longitude { get; set; }

            [JsonPropertyName("route-id")]
            public string? RouteId { get; set; }

            [JsonPropertyName("cctv-url")]
            public string? CctvUrl { get; set; }

            [JsonPropertyName("cctv-other")]
            public string? CctvOther { get; set; }

            [JsonPropertyName("last-update-time")]
            public string? LastUpdateTime { get; set; }
        }
    }
}
