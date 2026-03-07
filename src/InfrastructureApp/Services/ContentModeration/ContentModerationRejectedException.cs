/**This represents an error/exception type

used when a caller wants to stop the workflow

lets higher layers distinguish moderation rejection from other failures

That is also a separate concept from the service itself.**/

using System;

namespace InfrastructureApp.Services.ContentModeration;

public sealed class ContentModerationRejectedException : Exception
{
    public string? Category { get; }

    public ContentModerationRejectedException(string message, string? category = null)
        : base(message)
    {
        Category = category;
    }
}