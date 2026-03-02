//moderation class

namespace InfrastructureApp.Services.ContentModeration;

public sealed record ContentModerationResult(bool Performed, bool IsAllowed, bool Flagged, string? Reason = null);