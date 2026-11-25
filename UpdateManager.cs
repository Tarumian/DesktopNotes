using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hushak;

public class UpdateInfo
{
    public bool IsUpdateAvailable { get; set; }
    public string CurrentVersion { get; set; } = "1.5.15";
    public string LatestVersion { get; set; } = string.Empty;
    public string PageUrl { get; set; } = UpdateManager.DefaultWebsiteUrl;
    public string DownloadUrl { get; set; } = UpdateManager.DefaultWebsiteUrl;
    public string? Sha256 { get; set; }
    public string ReleaseDate { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public static class UpdateManager
{
    public const string DefaultWebsiteUrl = "https://tarumian.am/hushak/";
    public const string DefaultManifestUrl = "https://gitlab.com/Tarumian/hushak/-/raw/main/version.json";

    private static readonly HttpClient Http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"Hushak/{GetCurrentVersion()}");
        return client;
    }

    public static string GetCurrentVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var ver = asm.GetName().Version;
        if (ver != null)
        {
            if (ver.Revision > 0)
                return $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}";
            if (ver.Build >= 0)
                return $"{ver.Major}.{ver.Minor}.{ver.Build}";
            return $"{ver.Major}.{ver.Minor}";
        }
        return "1.5.15";
    }

    public static async Task<UpdateInfo> CheckForUpdatesAsync(
        string? customManifestUrl = null,
        CancellationToken cancellationToken = default)
    {
        string currentVerStr = GetCurrentVersion();
        string manifestUrl = IsHttpUrl(customManifestUrl) &&
                             customManifestUrl!.Trim().EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? customManifestUrl.Trim()
            : DefaultManifestUrl;

        try
        {
            using var response = await Http.GetAsync(manifestUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string latestVerStr = GetJsonString(root, "version") ?? currentVerStr;
            string pageUrl = SanitizeHttpUrl(GetJsonString(root, "pageUrl"), DefaultWebsiteUrl);
            string downloadUrl = SanitizeHttpUrl(GetJsonString(root, "downloadUrl"), pageUrl);
            string? sha256 = GetJsonString(root, "sha256");
            string releaseDate = GetJsonString(root, "releaseDate") ?? string.Empty;
            string releaseNotes = ExtractReleaseNotes(root);

            bool hasUpdate = CompareVersions(latestVerStr, currentVerStr) > 0;

            return new UpdateInfo
            {
                IsUpdateAvailable = hasUpdate,
                CurrentVersion = currentVerStr,
                LatestVersion = latestVerStr,
                PageUrl = pageUrl,
                DownloadUrl = downloadUrl,
                Sha256 = sha256,
                ReleaseDate = releaseDate,
                ReleaseNotes = string.IsNullOrWhiteSpace(releaseNotes)
                    ? LocalizationManager.Get("WebsitePromptDesc")
                    : releaseNotes
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UpdateInfo
            {
                IsUpdateAvailable = false,
                CurrentVersion = currentVerStr,
                LatestVersion = string.Empty,
                PageUrl = DefaultWebsiteUrl,
                DownloadUrl = DefaultWebsiteUrl,
                ReleaseNotes = LocalizationManager.Get("WebsitePromptDesc"),
                ErrorMessage = ex.Message
            };
        }
    }

    public static async Task<string> DownloadInstallerAsync(
        string downloadUrl,
        string? expectedSha256 = null,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsHttpUrl(downloadUrl))
            throw new InvalidOperationException("Invalid download URL.");

        using var response = await Http.GetAsync(
            downloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        string fileName = "Hushak_Setup.exe";
        if (Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri))
        {
            string candidate = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(candidate) &&
                candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                fileName = candidate;
            }
        }

        string tempPath = Path.Combine(Path.GetTempPath(), fileName);

        long? totalBytes = response.Content.Headers.ContentLength;
        await using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            byte[] buffer = new byte[81920];
            long totalRead = 0;
            int lastPercent = -1;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalRead += bytesRead;

                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    int percent = (int)Math.Clamp(totalRead * 100L / totalBytes.Value, 0, 100);
                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        progress?.Report(percent);
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(expectedSha256))
        {
            string actualSha256 = await ComputeSha256Async(tempPath, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(actualSha256, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(tempPath); } catch { }
                throw new InvalidOperationException(LocalizationManager.Get("UpdateHashMismatchMsg"));
            }
        }

        progress?.Report(100);
        return tempPath;
    }

    public static void LaunchInstaller(string installerPath)
    {
        if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
            throw new FileNotFoundException("Installer file not found.", installerPath);

        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true
        });
    }

    public static void OpenWebsite(string? url = null)
    {
        string target = SanitizeHttpUrl(url, DefaultWebsiteUrl);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = DefaultWebsiteUrl,
                UseShellExecute = true
            });
        }
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    private static int CompareVersions(string a, string b)
    {
        if (TryParseNormalizedVersion(a, out var va) && TryParseNormalizedVersion(b, out var vb))
            return va.CompareTo(vb);

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseNormalizedVersion(string? raw, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string cleaned = raw.Trim().TrimStart('v', 'V');
        int dashIdx = cleaned.IndexOf('-');
        if (dashIdx >= 0)
            cleaned = cleaned[..dashIdx];

        if (!Version.TryParse(cleaned, out var parsed) || parsed == null)
            return false;

        version = new Version(
            Math.Max(0, parsed.Major),
            Math.Max(0, parsed.Minor),
            Math.Max(0, parsed.Build),
            Math.Max(0, parsed.Revision));
        return true;
    }

    private static bool IsHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        string trimmed = url.Trim();
        return trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeHttpUrl(string? url, string fallback)
    {
        return IsHttpUrl(url) ? url!.Trim() : fallback;
    }

    private static string? GetJsonString(JsonElement root, string name)
    {
        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase) &&
                prop.Value.ValueKind == JsonValueKind.String)
            {
                return prop.Value.GetString();
            }
        }
        return null;
    }

    private static string ExtractReleaseNotes(JsonElement root)
    {
        foreach (var prop in root.EnumerateObject())
        {
            if (!string.Equals(prop.Name, "releaseNotes", StringComparison.OrdinalIgnoreCase))
                continue;

            if (prop.Value.ValueKind == JsonValueKind.String)
                return prop.Value.GetString() ?? string.Empty;

            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                string lang = LocalizationManager.Instance.CurrentLanguage;
                return GetJsonString(prop.Value, lang)
                    ?? GetJsonString(prop.Value, "hy")
                    ?? GetJsonString(prop.Value, "en")
                    ?? string.Empty;
            }
        }
        return string.Empty;
    }
}
