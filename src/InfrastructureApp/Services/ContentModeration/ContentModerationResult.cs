//moderation class
/**This is just a data contract

what was the result?

was moderation performed?

was it allowed?

was it flagged?

what was the reason?

It is not behavior-heavy. It is a value object / DTO-like result type.**/

namespace InfrastructureApp.Services.ContentModeration;

public sealed record ContentModerationResult(bool Performed, bool IsAllowed, bool Flagged, string? Reason = null);