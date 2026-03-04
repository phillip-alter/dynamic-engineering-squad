using System.Threading;
using System.Threading.Tasks;

namespace InfrastructureApp.Services.Moderation;

public interface IContentModerationService
{
    Task<ModerationResult> CheckAsync(string text, CancellationToken ct = default);
}