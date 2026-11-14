using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace AccessibleFootball2026.App;

/// <summary>Expected content of one downloaded update file, from its published JSON hash record.</summary>
public sealed record GameUpdateHashRecord(long Bytes, string Sha256, bool AssetsChanged, long ExeBytes, string? ExeSha256);

/// <summary>
/// Downloads, verifies, stages and applies official game updates. Settings live outside the
/// install folder and are never touched; audio/data are only ever replaced by the full
/// installer, never by a code patch. A code patch is staged beside the game and swapped in
/// only while the game process is not running (next launch, or a tiny self-copy applier),
/// so an active match is never interrupted and a half-written file can never be played.
/// </summary>
public static class GameUpdateDownloader
{
    public const string PendingDirectoryName = "pending";
    private const int MaxAttempts = 3;
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(30) };

    public static string UpdatesRoot(string installDir) => Path.Combine(installDir, "Updates");
    public static string PendingRoot(string installDir) => Path.Combine(UpdatesRoot(installDir), PendingDirectoryName);

    /// <summary>Parses a published hash record; expectedArtifact must match the record's artifact name.</summary>
    public static GameUpdateHashRecord? ParseHashRecord(string json, string expectedArtifact)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (!root.TryGetProperty("artifact", out var artifact) || artifact.GetString() != expectedArtifact) return null;
            if (!root.TryGetProperty("bytes", out var bytesNode) || !bytesNode.TryGetInt64(out long bytes) || bytes <= 0) return null;
            if (!root.TryGetProperty("sha256", out var shaNode) || !IsSha256(shaNode.GetString())) return null;
            bool assetsChanged = root.TryGetProperty("assets_changed", out var changedNode) &&
                (changedNode.ValueKind == JsonValueKind.True);
            long exeBytes = 0; string? exeSha = null;
            if (root.TryGetProperty("exe_bytes", out var exeBytesNode)) exeBytesNode.TryGetInt64(out exeBytes);
            if (root.TryGetProperty("exe_sha256", out var exeShaNode) && IsSha256(exeShaNode.GetString())) exeSha = exeShaNode.GetString();
            return new(bytes, shaNode.GetString()!.ToLowerInvariant(), assetsChanged, exeBytes, exeSha);
        }
        catch (JsonException) { return null; }
    }

    private static bool IsSha256(string? value) =>
        value != null && value.Length == 64 && value.All(Uri.IsHexDigit);

    /// <summary>
    /// Downloads the offer's asset and its hash record into Updates/downloads, resuming any partial
    /// file, then verifies size and SHA256. Returns the verified local file path.
    /// progress receives whole percentages (0-100); it is invoked on the caller's context.
    /// </summary>
    public static async Task<string> DownloadAsync(GameUpdateOffer offer, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        string installDir = AppContext.BaseDirectory;
        string downloadDir = Path.Combine(UpdatesRoot(installDir), "downloads");
        Directory.CreateDirectory(downloadDir);

        string recordText;
        using (var request = new HttpRequestMessage(HttpMethod.Get, offer.HashRecordAsset.DownloadUrl))
        {
            request.Headers.UserAgent.ParseAdd("AccessibleFootball2026/" + GameUpdateChecker.CurrentVersion);
            using var response = await Client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            recordText = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        var record = ParseHashRecord(recordText, offer.Asset.Name)
            ?? throw new InvalidDataException("Update hash record is missing or invalid.");
        if (record.Bytes != offer.Asset.Size)
            throw new InvalidDataException("Update hash record does not match the published file size.");

        string finalPath = Path.Combine(downloadDir, offer.Asset.Name);
        if (await VerifyFileAsync(finalPath, record, cancellationToken)) { progress?.Report(100); return finalPath; }

        string partPath = finalPath + ".part";
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long existing = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;
            if (existing > record.Bytes) File.Delete(partPath);
            else if (existing == record.Bytes && await VerifyFileAsync(partPath, record, cancellationToken)) { File.Move(partPath, finalPath, true); return finalPath; }

            using var request = new HttpRequestMessage(HttpMethod.Get, offer.Asset.DownloadUrl);
            request.Headers.UserAgent.ParseAdd("AccessibleFootball2026/" + GameUpdateChecker.CurrentVersion);
            if (existing > 0 && existing < record.Bytes) request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existing, null);
            try
            {
                using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                bool resumed = existing > 0 && response.StatusCode == System.Net.HttpStatusCode.PartialContent;
                if (!response.IsSuccessStatusCode) continue;
                if (existing > 0 && !resumed) File.Delete(partPath); // server ignored the range; start clean
                await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var target = new FileStream(partPath, resumed ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 256, true);
                var buffer = new byte[1024 * 256];
                long written = resumed ? existing : 0;
                int lastReported = -1;
                while (true)
                {
                    int read = await source.ReadAsync(buffer, cancellationToken);
                    if (read == 0) break;
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    written += read;
                    int percent = (int)(written * 100 / record.Bytes);
                    if (percent != lastReported) { lastReported = percent; progress?.Report(percent); }
                }
            }
            catch (HttpRequestException) when (attempt < MaxAttempts) { continue; }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < MaxAttempts) { continue; }
            if (await VerifyFileAsync(partPath, record, cancellationToken)) { File.Move(partPath, finalPath, true); return finalPath; }
        }
        throw new InvalidDataException("Downloaded update failed size or hash verification.");
    }

    private static async Task<bool> VerifyFileAsync(string path, GameUpdateHashRecord record, CancellationToken cancellationToken)
    {
        if (!File.Exists(path) || new FileInfo(path).Length != record.Bytes) return false;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 256, true);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
        return hash == record.Sha256;
    }

    /// <summary>
    /// Stages a verified code patch into Updates/pending: extracts the fresh executable and checks
    /// it against the record's exe hash. Nothing in the live install is touched yet.
    /// </summary>
    public static async Task StagePatchAsync(string zipPath, GameUpdateOffer offer, CancellationToken cancellationToken)
    {
        string recordText;
        using (var request = new HttpRequestMessage(HttpMethod.Get, offer.HashRecordAsset.DownloadUrl))
        {
            request.Headers.UserAgent.ParseAdd("AccessibleFootball2026/" + GameUpdateChecker.CurrentVersion);
            using var response = await Client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            recordText = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        var record = ParseHashRecord(recordText, offer.Asset.Name)
            ?? throw new InvalidDataException("Update hash record is missing or invalid.");
        if (record.AssetsChanged) throw new InvalidDataException("This patch does not cover changed game content.");

        string installDir = AppContext.BaseDirectory;
        string pending = PendingRoot(installDir);
        if (Directory.Exists(pending)) Directory.Delete(pending, true);
        Directory.CreateDirectory(pending);
        using (var archive = ZipFile.OpenRead(zipPath))
        {
            var entry = archive.GetEntry("AccessibleFootball2026.exe")
                ?? throw new InvalidDataException("Code patch does not contain the game executable.");
            if (entry.Length != record.ExeBytes) throw new InvalidDataException("Patched executable size mismatch.");
            entry.ExtractToFile(Path.Combine(pending, entry.Name), true);
        }
        string exePath = Path.Combine(pending, "AccessibleFootball2026.exe");
        if (record.ExeSha256 != null)
        {
            await using var stream = File.OpenRead(exePath);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
            if (hash != record.ExeSha256) { Directory.Delete(pending, true); throw new InvalidDataException("Patched executable failed hash verification."); }
        }
        await File.WriteAllTextAsync(Path.Combine(pending, "update.json"),
            JsonSerializer.Serialize(new { tag = "game-v" + offer.Version, version = offer.Version }), cancellationToken);
    }

    /// <summary>True when a staged update is waiting to be applied.</summary>
    public static bool HasPendingUpdate(string installDir) =>
        File.Exists(Path.Combine(PendingRoot(installDir), "AccessibleFootball2026.exe"));

    /// <summary>
    /// Applies a staged code patch over the install directory. Must run while the game itself is
    /// not running (at process start, before audio or windows are created, or from the applier
    /// copy after the game has exited). Settings, audio, database and saved data are untouched.
    /// </summary>
    public static void ApplyPendingUpdateIfAny(string installDir)
    {
        string pending = PendingRoot(installDir);
        string stagedExe = Path.Combine(pending, "AccessibleFootball2026.exe");
        if (!File.Exists(stagedExe)) return;
        string targetExe = Path.Combine(installDir, "AccessibleFootball2026.exe");
        string backup = Path.Combine(UpdatesRoot(installDir), "AccessibleFootball2026.previous.exe");
        string temp = Path.Combine(installDir, "AccessibleFootball2026.update-tmp.exe");
        try
        {
            if (File.Exists(targetExe)) File.Copy(targetExe, backup, true);
            // Copy beside the target first, then swap in place: a failed copy never leaves a
            // half-written game executable, and the previous copy stays runnable.
            File.Copy(stagedExe, temp, true);
            File.Move(temp, targetExe, true);
            Directory.Delete(pending, true);
        }
        catch (IOException)
        {
            // Leave the pending folder intact so the update is retried on the next launch.
        }
        catch (UnauthorizedAccessException)
        {
            // Leave the pending folder intact so the update is retried on the next launch.
        }
    }

    /// <summary>
    /// Restarts the game through a temporary copy of itself that waits for this process to exit,
    /// applies the staged patch, then relaunches the real game. Called only from the main menu,
    /// never during a match.
    /// </summary>
    public static void RestartAndApplyPendingUpdate()
    {
        string installDir = AppContext.BaseDirectory;
        string currentExe = Environment.ProcessPath ?? Path.Combine(installDir, "AccessibleFootball2026.exe");
        string applier = Path.Combine(UpdatesRoot(installDir), "AccessibleFootball2026-ApplyUpdate.exe");
        File.Copy(currentExe, applier, true);
        Process.Start(new ProcessStartInfo(applier,
            $"--apply-update \"{installDir}\" {Environment.ProcessId} \"{currentExe}\"")
        { UseShellExecute = false, WorkingDirectory = installDir });
    }

    /// <summary>Entry point of the temporary applier copy: wait, apply, relaunch, remove itself.</summary>
    public static int ApplyUpdateEntry(string[] args)
    {
        if (args.Length < 4) return 2;
        string installDir = args[1];
        if (!int.TryParse(args[2], out int gamePid)) return 2;
        string relaunchExe = args[3];
        try
        {
            for (int waited = 0; waited < 600; waited++)
            {
                try { if (Process.GetProcessById(gamePid).HasExited) break; }
                catch (ArgumentException) { break; }
                Thread.Sleep(500);
            }
            ApplyPendingUpdateIfAny(installDir);
            try { Process.Start(new ProcessStartInfo(relaunchExe) { UseShellExecute = true, WorkingDirectory = installDir }); }
            catch (System.ComponentModel.Win32Exception) { /* the player can relaunch the game normally */ }
            // Best-effort self cleanup on the next launch cycle.
            try
            {
                string self = Environment.ProcessPath ?? Path.Combine(UpdatesRoot(installDir), "AccessibleFootball2026-ApplyUpdate.exe");
                Process.Start(new ProcessStartInfo("cmd.exe", $"/c ping 127.0.0.1 -n 3 > nul & del \"{self}\"")
                { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false });
            }
            catch (System.ComponentModel.Win32Exception) { /* the leftover applier copy is harmless */ }
            return 0;
        }
        catch (IOException) { return 1; }
        catch (UnauthorizedAccessException) { return 1; }
    }

    /// <summary>Launches a verified full installer and lets it take over; the caller closes the game first.</summary>
    public static void LaunchInstaller(string installerPath, string installDir) =>
        Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true, WorkingDirectory = installDir });
}
