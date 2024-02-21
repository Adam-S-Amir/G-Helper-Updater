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
        string processName = "GHelper.exe";

        KillProcessByName(processName);

        if (File.Exists("GHelper.exe") || File.Exists("GHelper.zip"))
        {
            Console.Write("Files 'GHelper.exe' and/or 'GHelper.zip' already exist. Do you want to delete them? (y/n): ");
            var userResponse = Console.ReadLine();

            if (userResponse?.Trim().ToLower() == "y")
            {
                DeleteFiles();
            }
            else
            {
                Console.WriteLine("Program terminated by user.");
                return;
            }
        }

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
    static void KillProcessByName(string processName)
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                foreach (Process process in processes)
                {
                    process.Kill();
                }
                foreach (Process process in processes)
                {
                    process.WaitForExit();
                    Console.WriteLine($"Process '{processName}' with PID {process.Id} killed successfully.");
                }
            }
            else
            {
                Console.WriteLine($"No processes found with the name '{processName}'.");
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Error: {ex.Message} (Invalid operation)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    static void DeleteFiles()
    {
        string exeFilePath = "GHelper.exe";
        string zipFilePath = "GHelper.zip";
        try
        {
            if (File.Exists(exeFilePath))
            {
                File.Delete(exeFilePath);
                Console.WriteLine($"File '{exeFilePath}' deleted successfully.");
            }
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
                Console.WriteLine($"File '{zipFilePath}' deleted successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
