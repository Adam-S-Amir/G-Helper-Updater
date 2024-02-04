using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task Main()
    {
        var repositoryOwner = "seerge";
        var repositoryName = "g-helper";
        var downloadUrl = await GetLatestReleaseDownloadUrl(repositoryOwner, repositoryName);

        if (!string.IsNullOrEmpty(downloadUrl))
        {
            Console.WriteLine($"Download URL: {downloadUrl}");
            var localFilePath = "GHelper.zip";
            await DownloadFile(downloadUrl, localFilePath);
            Console.WriteLine($"File downloaded and saved to: {localFilePath}");

            var extractDirectory = Directory.GetCurrentDirectory();
            await ExtractZipFile(localFilePath, extractDirectory);
            Console.WriteLine($"Contents extracted to: {extractDirectory}");

            File.Delete(localFilePath);
            Console.WriteLine($"Downloaded zip file deleted.");

            var gHelperPath = Path.Combine(extractDirectory, "GHelper.exe");

            if (File.Exists(gHelperPath))
            {
                Process.Start(gHelperPath);
                Console.WriteLine("GHelper.exe started.");
            }
            else
            {
                Console.WriteLine("Error: GHelper.exe not found.");
            }
        }
        else
        {
            Console.WriteLine("Error: Unable to retrieve download URL.");
        }
    }

    static async Task<string?> GetLatestReleaseDownloadUrl(string owner, string repo)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "HttpClient");

                var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
                var response = await client.GetStringAsync(apiUrl);
                var json = JObject.Parse(response);

                var downloadUrl = json["assets"]?
                    .FirstOrDefault(a => a["browser_download_url"]?.ToString()?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ?? false)?
                    ["browser_download_url"]?.ToString();
                return downloadUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    static async Task DownloadFile(string url, string destinationFilePath)
    {
        using (HttpClient client = new HttpClient())
        {
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(destinationFilePath))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }
    }

    static async Task ExtractZipFile(string zipFilePath, string extractDirectory)
    {
        await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, extractDirectory));
    }
}
