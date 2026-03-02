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