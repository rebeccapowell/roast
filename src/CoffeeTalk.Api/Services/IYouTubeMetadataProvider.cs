using System.Threading;
using System.Threading.Tasks;

namespace CoffeeTalk.Api.Services;

public interface IYouTubeMetadataProvider
{
    Task<YouTubeVideoMetadata?> TryGetMetadataAsync(string videoId, CancellationToken cancellationToken);
}

public sealed record YouTubeVideoMetadata(string? Title, string? ThumbnailUrl);
