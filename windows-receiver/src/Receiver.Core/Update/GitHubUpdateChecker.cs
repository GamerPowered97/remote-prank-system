using System.Net.Http.Headers;
using System.Text.Json;
using Receiver.Core.Networking;

namespace Receiver.Core.Update;

public class GitHubUpdateChecker
{
    private readonly string _repoOwner;
    private readonly string _repoName;
    private readonly string _currentVersion;
    private readonly HttpClient _httpClient;

    public GitHubUpdateChecker(string currentVersion = "1.0.0", string repoOwner = "GamerPowered97", string repoName = "remote-prank-system")
    {
        _currentVersion = currentVersion.TrimStart('v');
        _repoOwner = repoOwner;
        _repoName = repoName;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RemotePrankReceiver", _currentVersion));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    public async Task<UpdateStatusMessage> CheckForUpdatesAsync()
    {
        var result = new UpdateStatusMessage
        {
            CurrentVersion = _currentVersion
        };

        try
        {
            string url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                result.HasUpdate = false;
                result.Error = $"GitHub API returned {response.StatusCode}: {response.ReasonPhrase}";
                return result;
            }

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tagName = root.GetProperty("tag_name").GetString() ?? "";
            string latestVersionStr = tagName.TrimStart('v');
            string body = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";

            result.LatestVersion = latestVersionStr;
            result.ReleaseNotes = body;

            // Find executable asset download url
            string downloadUrl = "";
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueArray)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(downloadUrl) && root.TryGetProperty("html_url", out var htmlUrl))
            {
                downloadUrl = htmlUrl.GetString() ?? "";
            }

            result.DownloadUrl = downloadUrl;

            // Compare versions
            if (Version.TryParse(_currentVersion, out var currVer) && Version.TryParse(latestVersionStr, out var newVer))
            {
                result.HasUpdate = newVer > currVer;
            }
            else
            {
                result.HasUpdate = !string.Equals(_currentVersion, latestVersionStr, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            result.HasUpdate = false;
            result.Error = ex.Message;
        }

        return result;
    }

    public async Task<string> DownloadUpdateAsync(string downloadUrl)
    {
        string tempFilePath = Path.Combine(Path.GetTempPath(), $"RemotePrankReceiver_update_{Guid.NewGuid():N}.exe");
        using var stream = await _httpClient.GetStreamAsync(downloadUrl);
        using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
        return tempFilePath;
    }

    private static readonly JsonValueKind JsonValueArray = JsonValueKind.Array;
}
