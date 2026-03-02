using System.Threading;
using System.Threading.Tasks;

namespace InfrastructureApp.Services.ContentModeration;

public interface IContentModerationService
{
    Task<ContentModerationResult> CheckAsync(string text, CancellationToken ct = default);
}