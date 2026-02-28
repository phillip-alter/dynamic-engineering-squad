namespace InfrastructureApp.Services.Moderation;

public sealed record ModerationResult(bool IsAllowed, bool Flagged, string? ReasonCategory = null
);