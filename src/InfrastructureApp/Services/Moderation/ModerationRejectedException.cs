using System;

namespace InfrastructureApp.Services.Moderation;

public sealed class ModerationRejectedException : Exception
{
    public string? Category { get; }

    public ModerationRejectedException(string message, string? category = null)
        : base(message)
    {
        Category = category;
    }
}