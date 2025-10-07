using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoffeeTalk.Api.Services;

public sealed class YouTubeMetadataProvider : IYouTubeMetadataProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<YouTubeOptions> _options;
    private readonly ILogger<YouTubeMetadataProvider> _logger;

    public YouTubeMetadataProvider(
        HttpClient httpClient,
        IOptions<YouTubeOptions> options,
        ILogger<YouTubeMetadataProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<YouTubeVideoMetadata?> TryGetMetadataAsync(string videoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return null;
        }

        var apiKey = _options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        try
        {
            using var response = await _httpClient
                .GetAsync($"videos?part=snippet&id={videoId}&key={apiKey}", cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "YouTube metadata lookup failed for video {VideoId} with status {StatusCode}.",
                    videoId,
                    response.StatusCode);
                return null;
            }

            var payload = await response.Content
                .ReadFromJsonAsync<YouTubeVideosResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (payload?.Items is not { Length: > 0 } items)
            {
                return null;
            }

            var snippet = items[0]?.Snippet;
            if (snippet is null)
            {
                return null;
            }

            var title = snippet.Title?.Trim();
            var thumbnailUrl = snippet.Thumbnails is null
                ? null
                : GetPreferredUrl(snippet.Thumbnails);

            return new YouTubeVideoMetadata(title, thumbnailUrl);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load YouTube metadata for video {VideoId}.", videoId);
            return null;
        }
    }

    private sealed record YouTubeVideosResponse(YouTubeVideoItem?[]? Items);

    private sealed record YouTubeVideoItem(YouTubeSnippet? Snippet);

    private sealed record YouTubeSnippet(string? Title, YouTubeThumbnails? Thumbnails);

    private sealed record YouTubeThumbnails(
        YouTubeThumbnail? Maxres,
        YouTubeThumbnail? Standard,
        YouTubeThumbnail? High,
        YouTubeThumbnail? Medium,
        YouTubeThumbnail? Default);

    private sealed record YouTubeThumbnail(string? Url);

    private static string? GetPreferredUrl(YouTubeThumbnails thumbnails)
    {
        return FirstValid(
            thumbnails.Maxres?.Url,
            thumbnails.Standard?.Url,
            thumbnails.High?.Url,
            thumbnails.Medium?.Url,
            thumbnails.Default?.Url);
    }

    private static string? FirstValid(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
