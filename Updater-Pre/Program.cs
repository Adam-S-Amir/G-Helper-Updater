using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task Main()
    {

        string owner = "seerge";
        string repo = "g-helper";

        CheckAdmin();
        KillProcess();
        Purge();
        jq();
        await GetLatestReleaseInfo(owner, repo);
        await DownloadZip(owner, repo);
        UnZip();
        Finish();

    }

    static void CheckAdmin()
    {
        Console.WriteLine("Checking for elevated privileges...");
        // Get the current Windows identity
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        // Create a Windows principal object
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        // Check if the current user has administrative privileges
        bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
        // Print appropriate message based on the admin check
        if (isAdmin)
        {
            Console.WriteLine("Running with administrator privileges.\n");
        }
        else
        {
            Console.WriteLine("This script requires administrator privileges.");
            Console.WriteLine("Please run the script as an administrator.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            Environment.Exit(0);
        }
    }

    static void KillProcess()
    {

        Console.WriteLine("Searching for GHelper...");
        string processName = "GHelper";

        // Use Process.GetProcessesByName to check if the process is running
        Process[] processes = Process.GetProcessesByName(processName);

        // Check if the process is found
        if (processes.Length > 0)
        {
            Console.WriteLine("Process GHelper has been found.");
            Console.WriteLine("Killing GHelper...");

            // Kill the process
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(); // Optional: Wait for the process to exit
                    Console.WriteLine($"Process {processName} has been killed.\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to kill process {processName}: {ex.Message}\n");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey(true);
                    Environment.Exit(0);
                }
            }
        }
        else
        {
            Console.WriteLine("Process GHelper has not been found.\n");
        }
    }

    static void Purge()
    {
        // Get the full path of the directory where the executable is running
        string destinationFolder = AppDomain.CurrentDomain.BaseDirectory;

        // Specify the file name to search for
        string fileName = "GHelper.exe";
        Console.WriteLine("Searching for GHelper.exe in current folder...");

        // Search for the file recursively in the directory
        try
        {
            string[] files = Directory.GetFiles(destinationFolder, fileName, SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                foreach (string file in files)
                {
                    Console.WriteLine("GHelper.exe has been found.");
                    Console.WriteLine("Deleting file...");
                    // Delete the file
                    File.Delete(file);
                    Console.WriteLine("Deprecated file deleted successfully.\n");
                }
            }
            else
            {
                Console.WriteLine("GHelper.exe has not been found.\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}\n");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            Environment.Exit(0);
        }
    }

    static void jq()
    {
        string packageName = "jqlang.jq";
        Console.WriteLine("Searching for jq...");
        // Execute winget show command
        int exitCode = ExecuteCommand($"winget show {packageName}");
        if (exitCode != 0)
        {
            // jq is not installed, install it
            Console.WriteLine("jq not found, please wait...");
            ExecuteCommand($"powershell -Command \"winget install -e --id {packageName}\"");
        }
        else
        {
            // jq is already installed
            Console.WriteLine("jq has already been installed.\n");
        }
    }

    static int ExecuteCommand(string command)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                // Read exit code
                return process.ExitCode;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            Environment.Exit(0);
            return -1; // Return -1 or handle error as needed
        }
    }

    static async Task GetLatestReleaseInfo(string owner, string repo)
    {
        Console.WriteLine("Fetching latest GHelper pre-release...");

        // GitHub API URLs
        string releasesUrl = $"https://api.github.com/repos/{owner}/{repo}/releases";

        try
        {
            using (HttpClient client = new HttpClient())
            {
                // GitHub API requires a user-agent header
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                // Fetch the list of releases
                HttpResponseMessage response = await client.GetAsync(releasesUrl);
                response.EnsureSuccessStatusCode(); // Throw if not success

                string json = await response.Content.ReadAsStringAsync();
                JArray releases = JArray.Parse(json);

                // Extract download URL and tag name of the latest release
                string downloadUrl = releases[0]["assets"][1]["browser_download_url"].ToString();
                string tagName = releases[0]["tag_name"].ToString();

                // Output the information to the console
                if (downloadUrl != null)
                {
                    Console.WriteLine("Latest pre-release found.");
                    Console.WriteLine($"Downloading version \"{tagName}\"...");
                }
                else
                {
                    Console.WriteLine("No suitable asset found for download.");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey(true);
                    Environment.Exit(0);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Error: {ex.Message}\n");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}\n");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            Environment.Exit(0);
        }
    }

    static async Task DownloadZip(string owner, string repo)
    {
        string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases";

        try
        {
            using (HttpClient client = new HttpClient())
            {
                // GitHub API requires a user-agent header
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                // Fetch the latest release information
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode(); // Ensure success status code

                // Read the response content as a string
                string responseBody = await response.Content.ReadAsStringAsync();

                // Parse the JSON response as an array
                JArray releases = JArray.Parse(responseBody);
                string downloadUrl = releases[0]["assets"][1]["browser_download_url"].ToString();

                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    // Download the file
                    await DownloadFileAsync(downloadUrl, "GHelper.zip");

                    Console.WriteLine("Download successful.\n");
                }
                else
                {
                    Console.WriteLine("No suitable asset found for download.\n");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey(true);
                    Environment.Exit(0);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request error: {ex.Message}\n");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}\n");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            Environment.Exit(0);
        }
    }

    static async Task DownloadFileAsync(string url, string destination)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                {
                    using (Stream streamToWriteTo = File.Open(destination, FileMode.Create))
                    {
                        await streamToReadFrom.CopyToAsync(streamToWriteTo);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file: {ex.Message}\n");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            Environment.Exit(0);
        }
    }

    static void UnZip()
    {
        string zipFilePath = @"GHelper.zip";
        string extractPath = Directory.GetCurrentDirectory();

        Console.WriteLine("Extracting files from archive...");

        try
        {
            // Extract the contents of the zip file
            ZipFile.ExtractToDirectory(zipFilePath, extractPath, true);

            Console.WriteLine("Archive extraction successful.");
            Console.WriteLine("Deleting archive...");

            // Delete the zip file
            File.Delete(zipFilePath);

            Console.WriteLine("Archive deleted.\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}\n");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            Environment.Exit(0);
        }
    }

    static void Finish()
    {
        string destinationFolder = AppDomain.CurrentDomain.BaseDirectory; // Get the current directory

        Console.WriteLine("Launching GHelper.exe...");

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = $"{destinationFolder}GHelper.exe", // Ensure correct path format without "/"
                UseShellExecute = true
            };

            // Start the process
            Process process = Process.Start(startInfo);

            // Wait for the process to exit asynchronously
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) =>
            {
                // Ensure all console output is flushed
                Console.Out.Flush();

                // Check the exit code
                if (process.ExitCode != 0)
                {
                    Console.WriteLine("Failed to start GHelper.exe.");
                    Environment.Exit(1); // Exit with error code 1
                }
                else
                {
                    Console.WriteLine("GHelper.exe started successfully.");
                }

                // Ensure the console stays open to view the output
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey(true);
                Environment.Exit(0); // Normal exit
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            Environment.Exit(1); // Exit with error code 1
        }
    }

}
