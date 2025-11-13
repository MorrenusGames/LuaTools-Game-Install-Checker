using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LuaToolsGameChecker
{
    public class MorrenusDownloader
    {
        private const string DownloadUrlTemplate = "http://167.235.229.108/m/{0}";

        /// <summary>
        /// Downloads and extracts Morrenus files for the specified AppID
        /// </summary>
        public static async Task<bool> DownloadAndExtractAsync(string appId, Action<string> statusCallback = null)
        {
            try
            {
                var steamPath = SteamHelper.GetSteamInstallPath();
                if (string.IsNullOrEmpty(steamPath))
                    throw new Exception("Steam installation path not found");

                var luaFolder = Path.Combine(steamPath, "config", "stplug-in");
                var depotCacheFolder = Path.Combine(steamPath, "depotcache");

                // Ensure directories exist
                Directory.CreateDirectory(luaFolder);
                Directory.CreateDirectory(depotCacheFolder);

                statusCallback?.Invoke("Downloading Morrenus files...");

                // Download the archive
                var downloadUrl = string.Format(DownloadUrlTemplate, appId);
                var tempArchivePath = Path.Combine(Path.GetTempPath(), $"morrenus_{appId}.zip");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(120);
                    client.DefaultRequestHeaders.Add("User-Agent", "Morrenus-Denuvo-Check");

                    var response = await client.GetAsync(downloadUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            throw new Exception($"Morrenus files not found for AppID {appId}. The game may not be supported yet.");
                        else
                            throw new Exception($"Download failed with status: {response.StatusCode}");
                    }

                    var content = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(tempArchivePath, content);
                }

                statusCallback?.Invoke("Extracting files...");

                // Extract the archive
                using (var archive = ZipFile.OpenRead(tempArchivePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        var extension = Path.GetExtension(entry.Name).ToLower();
                        string destinationPath = null;

                        if (extension == ".lua")
                        {
                            destinationPath = Path.Combine(luaFolder, entry.Name);
                        }
                        else if (extension == ".manifest")
                        {
                            destinationPath = Path.Combine(depotCacheFolder, entry.Name);
                        }

                        if (destinationPath != null)
                        {
                            // Extract and overwrite if exists
                            entry.ExtractToFile(destinationPath, overwrite: true);
                            statusCallback?.Invoke($"Extracted: {entry.Name}");
                        }
                    }
                }

                // Clean up temp file
                try
                {
                    File.Delete(tempArchivePath);
                }
                catch { }

                statusCallback?.Invoke("Morrenus files installed successfully!");

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download/extract Morrenus files: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if Morrenus files exist for the specified AppID
        /// </summary>
        public static bool MorrenusFilesExist(string appId)
        {
            try
            {
                var steamPath = SteamHelper.GetSteamInstallPath();
                if (string.IsNullOrEmpty(steamPath))
                    return false;

                var luaFolder = Path.Combine(steamPath, "config", "stplug-in");
                if (!Directory.Exists(luaFolder))
                    return false;

                var luaFiles = Directory.GetFiles(luaFolder, "*.lua");
                foreach (var luaFile in luaFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(luaFile);
                        // Check for Morrenus signature: "-- <appid>'s Lua and Manifest Created by Morrenus"
                        if (Regex.IsMatch(content, $@"--\s*{appId}'s\s+Lua\s+and\s+Manifest\s+Created\s+by\s+Morrenus", RegexOptions.IgnoreCase))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
