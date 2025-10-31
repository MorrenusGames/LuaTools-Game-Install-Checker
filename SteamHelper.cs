using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SteamGameVerifier
{
    public class SteamHelper
    {
        /// <summary>
        /// Gets the Steam installation path from the Windows Registry
        /// </summary>
        public static string? GetSteamInstallPath()
        {
            try
            {
                // Try 64-bit registry first
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                if (key != null)
                {
                    var path = key.GetValue("InstallPath") as string;
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                        return path;
                }

                // Try 32-bit registry
                using var key32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
                if (key32 != null)
                {
                    var path = key32.GetValue("InstallPath") as string;
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                        return path;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read Steam installation path from registry: {ex.Message}");
            }

            throw new Exception("Steam installation not found in registry");
        }

        /// <summary>
        /// Parses the libraryfolders.vdf file to find all Steam library locations
        /// </summary>
        public static Dictionary<string, string> GetSteamLibraries(string steamPath)
        {
            var libraries = new Dictionary<string, string>();
            var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

            if (!File.Exists(vdfPath))
                throw new Exception($"libraryfolders.vdf not found at: {vdfPath}");

            var content = File.ReadAllText(vdfPath);

            // Parse the VDF format to extract library paths
            var pathRegex = new Regex(@"""path""\s+""([^""]+)""", RegexOptions.IgnoreCase);
            var matches = pathRegex.Matches(content);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var path = match.Groups[1].Value.Replace(@"\\", @"\");
                    if (Directory.Exists(path))
                    {
                        libraries[path] = path;
                    }
                }
            }

            return libraries;
        }

        /// <summary>
        /// Finds which Steam library contains the specified AppID
        /// </summary>
        public static string? FindGameLibrary(string appId, Dictionary<string, string> libraries)
        {
            foreach (var library in libraries.Keys)
            {
                var manifestPath = Path.Combine(library, "steamapps", $"appmanifest_{appId}.acf");
                if (File.Exists(manifestPath))
                    return library;
            }
            return null;
        }

        /// <summary>
        /// Parses an ACF manifest file and returns key-value pairs
        /// </summary>
        public static Dictionary<string, string> ParseAcfFile(string acfPath)
        {
            var data = new Dictionary<string, string>();

            if (!File.Exists(acfPath))
                throw new Exception($"ACF file not found: {acfPath}");

            var content = File.ReadAllText(acfPath);

            // Parse simple key-value pairs (not nested sections)
            var keyValueRegex = new Regex(@"""([^""]+)""\s+""([^""]+)""", RegexOptions.Multiline);
            var matches = keyValueRegex.Matches(content);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 2)
                {
                    var key = match.Groups[1].Value;
                    var value = match.Groups[2].Value;
                    data[key] = value;
                }
            }

            return data;
        }

        /// <summary>
        /// Gets information about a game from its AppID
        /// </summary>
        public static GameInfo GetGameInfo(string appId)
        {
            var steamPath = GetSteamInstallPath();
            if (steamPath == null)
                throw new Exception("Steam installation not found");

            var libraries = GetSteamLibraries(steamPath);
            var gameLibrary = FindGameLibrary(appId, libraries);

            if (gameLibrary == null)
                throw new Exception($"Game with AppID {appId} not found in any Steam library");

            var manifestPath = Path.Combine(gameLibrary, "steamapps", $"appmanifest_{appId}.acf");
            var manifestData = ParseAcfFile(manifestPath);

            if (!manifestData.ContainsKey("installdir"))
                throw new Exception("Install directory not found in manifest");

            var installDir = manifestData["installdir"];
            var gamePath = Path.Combine(gameLibrary, "steamapps", "common", installDir);

            if (!Directory.Exists(gamePath))
                throw new Exception($"Game directory not found: {gamePath}");

            return new GameInfo
            {
                AppId = appId,
                Name = manifestData.ContainsKey("name") ? manifestData["name"] : "Unknown",
                InstallDir = installDir,
                GamePath = gamePath,
                BuildId = manifestData.ContainsKey("buildid") ? manifestData["buildid"] : "Unknown",
                SizeOnDisk = manifestData.ContainsKey("SizeOnDisk") ? manifestData["SizeOnDisk"] : "0",
                LibraryPath = gameLibrary
            };
        }

        /// <summary>
        /// Calculates the actual folder size in bytes
        /// </summary>
        public static long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            var dirInfo = new DirectoryInfo(path);
            long size = 0;

            try
            {
                // Get size of all files in the directory
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += file.Length;
                    }
                    catch
                    {
                        // Skip files we can't access
                    }
                }
            }
            catch
            {
                // If we can't access the directory, return 0
            }

            return size;
        }

        /// <summary>
        /// Formats bytes to human-readable format
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Generates a simple folder structure tree (files and folders at root level only)
        /// </summary>
        public static string GenerateFolderTree(string path)
        {
            if (!Directory.Exists(path))
                return "Directory not found";

            var sb = new StringBuilder();
            var dirInfo = new DirectoryInfo(path);

            sb.AppendLine(dirInfo.Name + "\\");

            try
            {
                // Get directories (folders)
                var directories = dirInfo.GetDirectories().OrderBy(d => d.Name);
                foreach (var dir in directories)
                {
                    sb.AppendLine($"  [{dir.Name}]");
                }

                // Get files
                var files = dirInfo.GetFiles().OrderBy(f => f.Name);
                foreach (var file in files)
                {
                    sb.AppendLine($"  {file.Name}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  Error reading directory: {ex.Message}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Resets activation status for a game and restarts Steam
        /// </summary>
        public static void ResetActivationAndRestartSteam(string appId)
        {
            try
            {
                // Use Steam protocol to clear the Steam ID for this game
                // Format: steam://run/tool/clearsteamid/<appid>
                var clearCommand = $"steam://run/tool/clearsteamid/{appId}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = clearCommand,
                    UseShellExecute = true
                });

                // Wait for the command to be processed
                System.Threading.Thread.Sleep(2000);

                // Kill all Steam processes
                var steamProcesses = Process.GetProcessesByName("steam");
                foreach (var process in steamProcesses)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    catch { }
                }

                // Wait a moment for processes to fully close
                System.Threading.Thread.Sleep(2000);

                // Start Steam again
                var steamPath = GetSteamInstallPath();
                if (steamPath != null)
                {
                    var steamExe = Path.Combine(steamPath, "steam.exe");
                    if (File.Exists(steamExe))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = steamExe,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to reset activation and restart Steam: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads and formats a Lua file with proper indentation
        /// </summary>
        public static string ReadAndFormatLuaFile(string luaFilePath)
        {
            if (!File.Exists(luaFilePath))
                return $"Lua file not found: {luaFilePath}";

            var lines = File.ReadAllLines(luaFilePath);
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Indent lines that don't start with "addappid"
                if (trimmed.StartsWith("addappid", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine(trimmed);
                }
                else if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine("    " + trimmed);
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the ACF manifest file path for a specific AppID
        /// </summary>
        public static string? GetAcfFilePath(string appId)
        {
            try
            {
                var steamPath = GetSteamInstallPath();
                if (steamPath == null)
                    return null;

                var libraries = GetSteamLibraries(steamPath);
                var gameLibrary = FindGameLibrary(appId, libraries);

                if (gameLibrary == null)
                    return null;

                return Path.Combine(gameLibrary, "steamapps", $"appmanifest_{appId}.acf");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the LastUpdated timestamp from the ACF file
        /// </summary>
        public static long GetAcfLastUpdated(string appId)
        {
            try
            {
                var acfPath = GetAcfFilePath(appId);
                if (acfPath == null || !File.Exists(acfPath))
                    return 0;

                var data = ParseAcfFile(acfPath);
                if (data.ContainsKey("LastUpdated"))
                {
                    if (long.TryParse(data["LastUpdated"], out long timestamp))
                        return timestamp;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the verification flag file path for a specific AppID
        /// </summary>
        private static string GetVerificationFlagPath(string appId)
        {
            var reportsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LuaTools Reports");

            Directory.CreateDirectory(reportsDir);
            return Path.Combine(reportsDir, $".verification_pending_{appId}");
        }

        /// <summary>
        /// Creates a verification flag file with the current ACF timestamp
        /// </summary>
        public static void CreateVerificationFlag(string appId)
        {
            var flagPath = GetVerificationFlagPath(appId);
            var timestamp = GetAcfLastUpdated(appId);
            File.WriteAllText(flagPath, timestamp.ToString());
        }

        /// <summary>
        /// Checks if a verification flag exists for the AppID
        /// </summary>
        public static bool VerificationFlagExists(string appId)
        {
            var flagPath = GetVerificationFlagPath(appId);
            return File.Exists(flagPath);
        }

        /// <summary>
        /// Checks if verification has been completed by comparing ACF timestamp
        /// Returns true if verification is complete, false if still pending
        /// </summary>
        public static bool IsVerificationComplete(string appId)
        {
            var flagPath = GetVerificationFlagPath(appId);
            if (!File.Exists(flagPath))
                return true; // No flag means no verification pending

            try
            {
                var flagContent = File.ReadAllText(flagPath);
                if (long.TryParse(flagContent, out long originalTimestamp))
                {
                    var currentTimestamp = GetAcfLastUpdated(appId);
                    // If timestamp changed, verification is complete
                    return currentTimestamp > originalTimestamp;
                }
            }
            catch
            {
                // If we can't read the flag, assume verification is needed
                return false;
            }

            return false;
        }

        /// <summary>
        /// Deletes the verification flag file
        /// </summary>
        public static void DeleteVerificationFlag(string appId)
        {
            var flagPath = GetVerificationFlagPath(appId);
            if (File.Exists(flagPath))
            {
                try
                {
                    File.Delete(flagPath);
                }
                catch
                {
                    // Silently fail if we can't delete
                }
            }
        }

        /// <summary>
        /// Launches the Steam validation dialog for a specific AppID
        /// </summary>
        public static void LaunchSteamValidation(string appId)
        {
            try
            {
                var validateCommand = $"steam://validate/{appId}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = validateCommand,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to launch Steam validation: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the download tracking file path for a specific AppID
        /// </summary>
        private static string GetDownloadTrackingPath(string appId)
        {
            var reportsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LuaTools Reports");

            Directory.CreateDirectory(reportsDir);
            return Path.Combine(reportsDir, $".dl_{appId}.dat");
        }

        /// <summary>
        /// Records that Morrenus files were downloaded for an AppID
        /// </summary>
        public static void RecordMorrenusDownload(string appId)
        {
            var trackingPath = GetDownloadTrackingPath(appId);
            var timestamp = GetAcfLastUpdated(appId);
            File.WriteAllText(trackingPath, $"{timestamp}|0");
        }

        /// <summary>
        /// Marks that verification was completed for an AppID
        /// </summary>
        public static void MarkVerificationCompleted(string appId)
        {
            var trackingPath = GetDownloadTrackingPath(appId);
            if (File.Exists(trackingPath))
            {
                var parts = File.ReadAllText(trackingPath).Split('|');
                if (parts.Length > 0)
                {
                    File.WriteAllText(trackingPath, $"{parts[0]}|1");
                }
            }
        }

        /// <summary>
        /// Checks if Morrenus download was bypassed (downloaded but never verified)
        /// </summary>
        public static bool CheckBypassAttempt(string appId)
        {
            var trackingPath = GetDownloadTrackingPath(appId);
            if (!File.Exists(trackingPath))
                return false;

            try
            {
                var data = File.ReadAllText(trackingPath);
                var parts = data.Split('|');

                if (parts.Length >= 2)
                {
                    var verified = parts[1];
                    // If verified flag is "0", they bypassed verification
                    return verified == "0";
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }

    /// <summary>
    /// Represents information about a Steam game
    /// </summary>
    public class GameInfo
    {
        public string AppId { get; set; } = "";
        public string Name { get; set; } = "";
        public string InstallDir { get; set; } = "";
        public string GamePath { get; set; } = "";
        public string BuildId { get; set; } = "";
        public string SizeOnDisk { get; set; } = "";
        public string LibraryPath { get; set; } = "";
    }
}
