using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LuaToolsGameChecker
{
    public class UpdateManager
    {
        private const string GitHubReleaseUrl = "https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/latest/download/LuaToolsGameInstallChecker.exe";
        private const string CurrentVersion = "1.5.1";

        /// <summary>
        /// Checks if an update is available by comparing file sizes
        /// </summary>
        public static async Task<bool> IsUpdateAvailableAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                // Send HEAD request to get content length without downloading
                var request = new HttpRequestMessage(HttpMethod.Head, GitHubReleaseUrl);
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return false;

                // Get remote file size
                var remoteSize = response.Content.Headers.ContentLength ?? 0;

                // Get current exe size
                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExePath) || !File.Exists(currentExePath))
                    return false;

                var currentSize = new FileInfo(currentExePath).Length;

                // If sizes differ, update is available
                return remoteSize != currentSize && remoteSize > 0;
            }
            catch
            {
                // If check fails, assume no update available
                return false;
            }
        }

        /// <summary>
        /// Downloads and installs the update
        /// </summary>
        public static async Task<bool> DownloadAndInstallUpdateAsync(Action<int> progressCallback = null)
        {
            try
            {
                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExePath))
                    throw new Exception("Could not determine current executable path");

                var tempPath = Path.Combine(Path.GetTempPath(), "LuaToolsGameInstallChecker_Update.exe");
                var backupPath = currentExePath + ".bak";

                // Download the update
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(60);

                progressCallback?.Invoke(10);

                var response = await client.GetAsync(GitHubReleaseUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                progressCallback?.Invoke(30);

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        var progress = 30 + (int)((totalRead * 50.0) / totalBytes);
                        progressCallback?.Invoke(progress);
                    }
                }

                progressCallback?.Invoke(90);

                // Create a batch script to replace the exe
                var batchPath = Path.Combine(Path.GetTempPath(), "update_luatools.bat");
                var batchContent = $@"@echo off
timeout /t 2 /nobreak > nul
move /Y ""{currentExePath}"" ""{backupPath}"" > nul 2>&1
move /Y ""{tempPath}"" ""{currentExePath}"" > nul 2>&1
start """" ""{currentExePath}""
del ""{backupPath}"" > nul 2>&1
del ""%~f0"" > nul 2>&1
";

                File.WriteAllText(batchPath, batchContent);

                // Start the batch script and exit
                var psi = new ProcessStartInfo
                {
                    FileName = batchPath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);

                progressCallback?.Invoke(100);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download and install update: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current version string
        /// </summary>
        public static string GetCurrentVersion()
        {
            return CurrentVersion;
        }
    }
}
