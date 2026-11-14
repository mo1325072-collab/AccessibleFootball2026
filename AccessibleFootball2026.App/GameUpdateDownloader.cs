using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace AccessibleFootball2026.App;

/// <summary>
/// Expected content of one downloaded update file, from its published JSON hash record.
/// BaseVersion names the exact installed version a code patch was built from; the patch may
/// only ever touch that baseline. Full installer records leave it null: they serve any install.
/// </summary>
public sealed record GameUpdateHashRecord(long Bytes, string Sha256, bool AssetsChanged, long ExeBytes, string? ExeSha256, string? BaseVersion);

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
            string? baseVersion = root.TryGetProperty("base_version", out var baseNode) && baseNode.ValueKind == JsonValueKind.String
                ? baseNode.GetString() : null;
            return new(bytes, shaNode.GetString()!.ToLowerInvariant(), assetsChanged, exeBytes, exeSha, baseVersion);
        }
        catch (JsonException) { return null; }
    }

    private static bool IsSha256(string? value) =>
        value != null && value.Length == 64 && value.All(Uri.IsHexDigit);

    /// <summary>
    /// A code patch is safe only for the exact install it was built from: the record must name
    /// the installed version as its base, declare unchanged assets, and match the published
    /// bytes. assets_changed=false alone is never enough: an install older than the patch's
    /// baseline could carry different audio/data, so anything else takes the full installer.
    /// </summary>
    public static bool IsPatchCompatible(GameUpdateHashRecord record, long assetBytes, string installedVersion) =>
        !record.AssetsChanged && record.Bytes == assetBytes &&
        record.BaseVersion != null && record.BaseVersion == installedVersion;

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
        if (!IsPatchCompatible(record, offer.Asset.Size, GameUpdateChecker.CurrentVersion))
            throw new InvalidDataException("This patch was not built for the installed version; the full installer is required.");

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
    /// Applies a staged code patch over the install directory. Runs only from the applier copy
    /// after the game process has fully exited: the running game's own executable is locked on
    /// Windows, so a process can never swap its own image. Settings, audio, database and saved
    /// data are untouched. On any failure the staged update is left intact for the next try.
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
    /// Starts the temporary applier copy that waits for this process to exit, swaps in the
    /// staged patch and relaunches the game. Called only from the main menu or from startup
    /// before anything else loads, never during a match. Returns false when the applier could
    /// not be started; the staged update then simply waits for the next launch.
    /// </summary>
    public static bool TryStartApplier()
    {
        try
        {
            string installDir = AppContext.BaseDirectory;
            string currentExe = Environment.ProcessPath ?? Path.Combine(installDir, "AccessibleFootball2026.exe");
            string applier = Path.Combine(UpdatesRoot(installDir), "AccessibleFootball2026-ApplyUpdate.exe");
            File.Copy(currentExe, applier, true);
            return Process.Start(new ProcessStartInfo(applier,
                $"--apply-update \"{installDir}\" {Environment.ProcessId} \"{currentExe}\"")
            { UseShellExecute = false, WorkingDirectory = installDir }) != null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// True when the process is gone within the timeout. A vanished PID counts as exited.
    /// The swap must never run while the game lives: its executable is locked and a match may
    /// be in progress.
    /// </summary>
    public static bool WaitForProcessExit(int processId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try { if (Process.GetProcessById(processId).HasExited) return true; }
            catch (ArgumentException) { return true; }
            Thread.Sleep(250);
        }
        try { return Process.GetProcessById(processId).HasExited; }
        catch (ArgumentException) { return true; }
    }

    /// <summary>
    /// Entry point of the temporary applier copy: wait for the game to fully exit, apply the
    /// staged patch, relaunch with --after-update-attempt (so a failed swap cannot bounce the
    /// next process straight back into the applier), then remove itself.
    /// </summary>
    public static int ApplyUpdateEntry(string[] args)
    {
        if (args.Length < 4) return 2;
        string installDir = args[1];
        if (!int.TryParse(args[2], out int gamePid)) return 2;
        string relaunchExe = args[3];
        try
        {
            // The game is still running after the wait: leave the staged update for the next
            // launch instead of swapping over a locked executable or interrupting a match.
            if (!WaitForProcessExit(gamePid, TimeSpan.FromSeconds(60))) return 1;
            ApplyPendingUpdateIfAny(installDir);
            // Relaunch whether or not the swap landed: the player asked for a restart and gets
            // their game back, on the new build when the swap worked, otherwise unchanged.
            try { Process.Start(new ProcessStartInfo(relaunchExe, "--after-update-attempt") { UseShellExecute = true, WorkingDirectory = installDir }); }
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
