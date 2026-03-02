//moderation class

namespace InfrastructureApp.Services.Moderation;

public sealed record ModerationResult(bool Performed, bool IsAllowed, bool Flagged, string? Reason = null);