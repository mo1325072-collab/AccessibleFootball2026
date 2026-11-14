using System.Net.Http;
using System.Text.Json;

namespace AccessibleFootball2026.App;

public enum GameUpdateState { Available, Current, NoPublishedGame, Failed }
public enum GameUpdateDownloadKind { FullInstaller, CodePatch }

/// <summary>One downloadable release asset, already validated against the expected repository layout.</summary>
public sealed record GameUpdateAsset(string Name, long Size, string DownloadUrl);

/// <summary>
/// A concrete, verified downloadable update: what to download and the JSON hash record that verifies it.
/// The full installer pair is always carried too, so a patch that fails its record check can fall back.
/// </summary>
public sealed record GameUpdateOffer(
    GameUpdateDownloadKind Kind, string Version, string ReleaseUrl,
    GameUpdateAsset Asset, GameUpdateAsset HashRecordAsset,
    GameUpdateAsset InstallerAsset, GameUpdateAsset InstallerRecordAsset)
{
    public GameUpdateOffer AsFullInstaller() =>
        this with { Kind = GameUpdateDownloadKind.FullInstaller, Asset = InstallerAsset, HashRecordAsset = InstallerRecordAsset };
}

/// <summary>Result of checking the official game releases repository.</summary>
public sealed record GameUpdateResult(GameUpdateState State, string? Version = null, string? ReleaseUrl = null, GameUpdateOffer? Offer = null);

/// <summary>
/// Finds official game updates on the dedicated releases repository and chooses the smallest
/// verified download. A code patch (fresh executable only) is preferred when the release
/// declares that audio/data assets did not change; otherwise the full installer is required.
/// Nothing here installs silently: the player confirms every download and every install.
/// </summary>
public static class GameUpdateChecker
{
    public const string CurrentVersion = "2026.1";
    public const string Repository = "mo1325072-collab/Accessible-Football-Releases";
    public const string InstalledTag = "game-v" + CurrentVersion;
    public const string InstallerName = "AccessibleFootball2026-Full-Setup.exe";
    public const string InstallerRecordName = "AccessibleFootball2026-Full-Setup.json";
    public const string PatchName = "AccessibleFootball2026-CodePatch.zip";
    public const string PatchRecordName = "AccessibleFootball2026-CodePatch.json";
    private const string TagPrefix = "game-v";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };

    public static async Task<GameUpdateResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repository}/releases?per_page=100");
            request.Headers.UserAgent.ParseAdd("AccessibleFootball2026/" + CurrentVersion);
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            using var response = await Client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (result.State != GameUpdateState.Available || result.Offer == null) return result;
            return await ResolvePatchRecordAsync(result, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException or FormatException)
        {
            return new(GameUpdateState.Failed);
        }
    }

    /// <summary>
    /// When the release carries a code patch, fetch its small JSON record to learn whether assets
    /// changed and to obtain the expected bytes/SHA256 before any large download starts.
    /// If the record cannot be read or says assets changed, fall back to the full installer.
    /// </summary>
    private static async Task<GameUpdateResult> ResolvePatchRecordAsync(GameUpdateResult result, CancellationToken cancellationToken)
    {
        var offer = result.Offer!;
        if (offer.Kind != GameUpdateDownloadKind.CodePatch) return result;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, offer.HashRecordAsset.DownloadUrl);
            request.Headers.UserAgent.ParseAdd("AccessibleFootball2026/" + CurrentVersion);
            using var response = await Client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var record = GameUpdateDownloader.ParseHashRecord(await response.Content.ReadAsStringAsync(cancellationToken), offer.Asset.Name);
            if (record == null || record.AssetsChanged || record.Bytes != offer.Asset.Size)
                return result with { Offer = offer.AsFullInstaller() };
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException or FormatException)
        {
            return result with { Offer = offer.AsFullInstaller() };
        }
    }

    /// <summary>
    /// Pure release-list parse: returns the newest qualifying stable release and the preferred offer.
    /// Qualifying means: not draft, not prerelease, a game-v tag, an uploaded full installer,
    /// and its hash record. A code patch is offered only when both patch files are also uploaded.
    /// </summary>
    public static GameUpdateResult Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array) return new(GameUpdateState.Failed);
        string? bestTag = null;
        Version? bestVersion = null;
        DateTimeOffset bestPublished = DateTimeOffset.MinValue;
        string? releaseUrl = null;
        GameUpdateOffer? offer = null;
        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (release.ValueKind != JsonValueKind.Object ||
                !release.TryGetProperty("draft", out var draft) || draft.ValueKind != JsonValueKind.False ||
                !release.TryGetProperty("prerelease", out var prerelease) || prerelease.ValueKind != JsonValueKind.False ||
                !release.TryGetProperty("tag_name", out var tagNode) || tagNode.ValueKind != JsonValueKind.String) continue;
            string tag = tagNode.GetString()!;
            if (!tag.StartsWith(TagPrefix, StringComparison.Ordinal) || !Version.TryParse(tag[TagPrefix.Length..], out var version)) continue;
            if (!TryReadReleaseUrl(release, out string? url)) continue;
            if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) continue;
            var installer = FindAsset(assets, InstallerName);
            var installerRecord = FindAsset(assets, InstallerRecordName);
            if (installer == null || installerRecord == null) continue;
            var patch = FindAsset(assets, PatchName);
            var patchRecord = FindAsset(assets, PatchRecordName);
            DateTimeOffset published = release.TryGetProperty("published_at", out var publishedNode) &&
                publishedNode.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(publishedNode.GetString(), out var parsed) ? parsed : DateTimeOffset.MinValue;
            // Newest release wins; published_at is the primary order, version the tie-breaker.
            if (bestTag != null && (published < bestPublished || published == bestPublished && version <= bestVersion)) continue;
            bestTag = tag; bestVersion = version; bestPublished = published; releaseUrl = url;
            offer = patch != null && patchRecord != null
                ? new(GameUpdateDownloadKind.CodePatch, version.ToString(), url!, patch, patchRecord, installer, installerRecord)
                : new(GameUpdateDownloadKind.FullInstaller, version.ToString(), url!, installer, installerRecord, installer, installerRecord);
        }
        if (bestTag == null) return new(GameUpdateState.NoPublishedGame);
        return new(bestTag == InstalledTag ? GameUpdateState.Current : GameUpdateState.Available, bestVersion!.ToString(), releaseUrl, offer);
    }

    private static bool TryReadReleaseUrl(JsonElement release, out string? url)
    {
        url = null;
        if (!release.TryGetProperty("html_url", out var urlNode) || urlNode.ValueKind != JsonValueKind.String ||
            !Uri.TryCreate(urlNode.GetString(), UriKind.Absolute, out var parsed) ||
            parsed.Scheme != "https" || parsed.Host != "github.com" ||
            !parsed.AbsolutePath.StartsWith("/" + Repository + "/releases/tag/", StringComparison.Ordinal)) return false;
        url = parsed.AbsoluteUri;
        return true;
    }

    private static GameUpdateAsset? FindAsset(JsonElement assets, string expectedName)
    {
        foreach (var asset in assets.EnumerateArray())
        {
            if (asset.ValueKind != JsonValueKind.Object) continue;
            if (!asset.TryGetProperty("name", out var name) || name.GetString() != expectedName) continue;
            if (!asset.TryGetProperty("size", out var size) || !size.TryGetInt64(out var bytes) || bytes <= 0) continue;
            if (!asset.TryGetProperty("state", out var state) || state.GetString() != "uploaded") continue;
            if (!asset.TryGetProperty("browser_download_url", out var download) ||
                !Uri.TryCreate(download.GetString(), UriKind.Absolute, out var downloadUri) ||
                downloadUri.Scheme != "https" || downloadUri.Host != "github.com" ||
                !downloadUri.AbsolutePath.StartsWith("/" + Repository + "/releases/download/", StringComparison.Ordinal)) continue;
            return new(expectedName, bytes, downloadUri.AbsoluteUri);
        }
        return null;
    }
}
