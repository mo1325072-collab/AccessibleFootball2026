using System.Net.Http;
using System.Text.Json;

namespace AccessibleFootball2026.App;

public enum GameUpdateState { Available, Current, NoPublishedGame, Failed }
public sealed record GameUpdateResult(GameUpdateState State, string? Version = null, string? ReleaseUrl = null);

/// <summary>Checks published game installers only. Does not download or install anything.</summary>
public static class GameUpdateChecker
{
    public const string CurrentVersion = "2026.11.14";
    private const string Repository = "mo1325072-collab/AccessibleFootball2026";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20) };

    public static async Task<GameUpdateResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repository}/releases?per_page=100");
            request.Headers.UserAgent.ParseAdd("AccessibleFootball2026/" + CurrentVersion);
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            using var response = await Client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException or FormatException)
        {
            return new(GameUpdateState.Failed);
        }
    }

    public static GameUpdateResult Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array) return new(GameUpdateState.Failed);
        Version? newest = null;
        string? releaseUrl = null;
        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (release.ValueKind != JsonValueKind.Object ||
                !release.TryGetProperty("draft", out var draft) || draft.ValueKind != JsonValueKind.False ||
                !release.TryGetProperty("prerelease", out var prerelease) || prerelease.ValueKind != JsonValueKind.False ||
                !release.TryGetProperty("tag_name", out var tagNode) || tagNode.ValueKind != JsonValueKind.String) continue;
            string tag = tagNode.GetString()!;
            if (!tag.StartsWith("game-v", StringComparison.Ordinal) || !Version.TryParse(tag[6..], out var version)) continue;
            if (!release.TryGetProperty("html_url", out var urlNode) || urlNode.ValueKind != JsonValueKind.String ||
                !Uri.TryCreate(urlNode.GetString(), UriKind.Absolute, out var url) ||
                url.Scheme != "https" || url.Host != "github.com" ||
                !url.AbsolutePath.StartsWith("/" + Repository + "/releases/tag/", StringComparison.Ordinal)) continue;
            if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) continue;
            bool hasInstaller = assets.EnumerateArray().Any(a =>
                a.TryGetProperty("name", out var name) && name.GetString() == "AccessibleFootball2026-Full-Setup.exe" &&
                a.TryGetProperty("size", out var size) && size.TryGetInt64(out var bytes) && bytes > 0 &&
                a.TryGetProperty("state", out var state) && state.GetString() == "uploaded" &&
                a.TryGetProperty("browser_download_url", out var download) &&
                Uri.TryCreate(download.GetString(), UriKind.Absolute, out var downloadUri) &&
                downloadUri.Scheme == "https" && downloadUri.Host == "github.com" &&
                downloadUri.AbsolutePath.StartsWith("/" + Repository + "/releases/download/", StringComparison.Ordinal));
            if (!hasInstaller || newest != null && version <= newest) continue;
            newest = version;
            releaseUrl = url.AbsoluteUri;
        }
        if (newest == null) return new(GameUpdateState.NoPublishedGame);
        return new(newest > Version.Parse(CurrentVersion) ? GameUpdateState.Available : GameUpdateState.Current, newest.ToString(), releaseUrl);
    }
}
