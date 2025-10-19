using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace LuaToolsGameChecker
{
    public class SteamHelper
    {
        public static string? GetSteamInstallPath()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                if (key != null)
                {
                    var path = key.GetValue("InstallPath") as string;
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                        return path;
                }

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

        public static Dictionary<string, string> GetSteamLibraries(string steamPath)
        {
            var libraries = new Dictionary<string, string>();
            var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

            if (!File.Exists(vdfPath))
                throw new Exception($"libraryfolders.vdf not found at: {vdfPath}");

            var content = File.ReadAllText(vdfPath);
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

        public static Dictionary<string, string> ParseAcfFile(string acfPath)
        {
            var data = new Dictionary<string, string>();

            if (!File.Exists(acfPath))
                throw new Exception($"ACF file not found: {acfPath}");

            var content = File.ReadAllText(acfPath);
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

        public static long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            var dirInfo = new DirectoryInfo(path);
            long size = 0;

            try
            {
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += file.Length;
                    }
                    catch { }
                }
            }
            catch { }

            return size;
        }

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

        public static string GenerateFolderTree(string path)
        {
            if (!Directory.Exists(path))
                return "Directory not found";

            var sb = new StringBuilder();
            var dirInfo = new DirectoryInfo(path);

            sb.AppendLine(dirInfo.Name + "\\");

            try
            {
                var directories = dirInfo.GetDirectories().OrderBy(d => d.Name);
                foreach (var dir in directories)
                {
                    sb.AppendLine($"  [{dir.Name}]");
                }

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

        public static void ResetActivationAndRestartSteam(string appId)
        {
            try
            {
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

                System.Threading.Thread.Sleep(2000);

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

                System.Threading.Thread.Sleep(5000);
                TryLaunchGame(appId);
                System.Threading.Thread.Sleep(3000);

                var clearCommand = $"steam://run/tool/clearsteamid/{appId}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = clearCommand,
                    UseShellExecute = true
                });

                System.Threading.Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to restart Steam, launch game, and clear Steam ID: {ex.Message}");
            }
        }

        public static void TryLaunchGame(string appId)
        {
            try
            {
                var launchCommand = $"steam://rungameid/{appId}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = launchCommand,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public static string? FindLuaFileForAppId(string appId)
        {
            try
            {
                var steamPath = GetSteamInstallPath();
                if (steamPath == null)
                {
                    System.Windows.MessageBox.Show(
                        "ERROR: Steam installation path not found!\n\n" +
                        "Could not locate Steam installation in registry.\n\n" +
                        "Please ensure Steam is properly installed.",
                        "Steam Path Not Found",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return null;
                }

                var luaFolder = Path.Combine(steamPath, "config", "stplug-in");
                if (!Directory.Exists(luaFolder))
                {
                    System.Windows.MessageBox.Show(
                        $"ERROR: LuaTools directory not found!\n\n" +
                        $"Expected path: {luaFolder}\n\n" +
                        $"The 'stplug-in' folder does not exist.\n\n" +
                        $"Please install LuaTools plugin in Steam.",
                        "LuaTools Directory Not Found",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return null;
                }

                var luaFiles = Directory.GetFiles(luaFolder, "*.lua");

                if (luaFiles.Length == 0)
                {
                    System.Windows.MessageBox.Show(
                        $"ERROR: No Lua files found!\n\n" +
                        $"Directory exists: {luaFolder}\n\n" +
                        $"But no .lua files were found inside.\n\n" +
                        $"Please add a Lua configuration file from:\n" +
                        $"- Sage Bot\n" +
                        $"- Luie\n" +
                        $"- The plugin",
                        "No Lua Files Found",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return null;
                }

                foreach (var luaFile in luaFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(luaFile);
                        if (Regex.IsMatch(content, $@"(^|[\r\n])\s*(--\s*)?addappid\s*\(\s*[""']?{appId}[""']?\s*[,\)]", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                        {
                            return luaFile;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                System.Windows.MessageBox.Show(
                    $"ERROR: AppID {appId} not found in any Lua file!\n\n" +
                    $"Found {luaFiles.Length} Lua file(s) in:\n{luaFolder}\n\n" +
                    $"But none contain addappid({appId})\n\n" +
                    $"Please ensure you have the correct Lua file for this game.",
                    "AppID Not Found in Lua Files",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"ERROR: Unexpected error while searching for Lua file!\n\n" +
                    $"Error: {ex.Message}",
                    "Lua Search Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }

            return null;
        }

        public static void DisableDlcUpdates(string luaFilePath, string mainAppId)
        {
            if (!File.Exists(luaFilePath))
                throw new Exception($"Lua file not found: {luaFilePath}");

            var lines = File.ReadAllLines(luaFilePath);
            var modifiedLines = new List<string>();

            var fileContent = string.Join("\n", lines);
            if (!fileContent.Contains("morrenus", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(
                    "'morrenus' not found in the Lua file!\n\n" +
                    "This is not a valid LuaTools configuration file.\n\n" +
                    "Please refetch the file from:\n" +
                    "- Sage Bot\n" +
                    "- Luie\n" +
                    "- The plugin");
            }

            bool hasHeader = lines.Length > 0 && lines[0].Trim() == "-- LUATOOLS: UPDATES DISABLED!";
            if (!hasHeader)
            {
                modifiedLines.Add("-- LUATOOLS: UPDATES DISABLED!");
            }

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("--"))
                {
                    var uncommented = trimmed.Substring(2).Trim();

                    if (uncommented.StartsWith("setManifestid", StringComparison.OrdinalIgnoreCase))
                    {
                        var indent = line.Substring(0, line.Length - line.TrimStart().Length);
                        modifiedLines.Add(indent + uncommented);
                        continue;
                    }

                    modifiedLines.Add(line);
                    continue;
                }

                if (trimmed.StartsWith("addappid", StringComparison.OrdinalIgnoreCase))
                {
                    var depotMatch = Regex.Match(trimmed, @"addappid\s*\(\s*(\d+)\s*,\s*\d+\s*,\s*""[^""]+""", RegexOptions.IgnoreCase);

                    if (depotMatch.Success)
                    {
                        var depotAppId = depotMatch.Groups[1].Value;

                        if (depotAppId != mainAppId)
                        {
                            var indent = line.Substring(0, line.Length - line.TrimStart().Length);
                            modifiedLines.Add(indent + "-- " + trimmed);
                            continue;
                        }
                    }
                }

                modifiedLines.Add(line);
            }

            File.WriteAllLines(luaFilePath, modifiedLines);
        }

        public static void EnableDlcUpdates(string luaFilePath, string mainAppId)
        {
            if (!File.Exists(luaFilePath))
                throw new Exception($"Lua file not found: {luaFilePath}");

            var lines = File.ReadAllLines(luaFilePath);
            var modifiedLines = new List<string>();

            var fileContent = string.Join("\n", lines);
            if (!fileContent.Contains("morrenus", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(
                    "'morrenus' not found in the Lua file!\n\n" +
                    "This is not a valid LuaTools configuration file.\n\n" +
                    "Please refetch the file from:\n" +
                    "- Sage Bot\n" +
                    "- Luie\n" +
                    "- The plugin");
            }

            bool skipFirst = false;
            if (lines.Length > 0 && lines[0].Trim() == "-- LUATOOLS: UPDATES DISABLED!")
            {
                skipFirst = true;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0 && skipFirst)
                {
                    continue;
                }

                var line = lines[i];
                var trimmed = line.Trim();

                if (trimmed.StartsWith("setManifestid", StringComparison.OrdinalIgnoreCase))
                {
                    var indent = line.Substring(0, line.Length - line.TrimStart().Length);
                    modifiedLines.Add(indent + "-- " + trimmed);
                    continue;
                }

                if (trimmed.StartsWith("--"))
                {
                    var uncommented = trimmed.Substring(2).Trim();

                    if (uncommented.StartsWith("addappid", StringComparison.OrdinalIgnoreCase))
                    {
                        var depotMatch = Regex.Match(uncommented, @"addappid\s*\(\s*(\d+)\s*,\s*\d+\s*,\s*""[^""]+""", RegexOptions.IgnoreCase);

                        if (depotMatch.Success)
                        {
                            var depotAppId = depotMatch.Groups[1].Value;

                            if (depotAppId != mainAppId)
                            {
                                var indent = line.Substring(0, line.Length - line.TrimStart().Length);
                                modifiedLines.Add(indent + uncommented);
                                continue;
                            }
                        }
                    }
                }

                modifiedLines.Add(line);
            }

            File.WriteAllLines(luaFilePath, modifiedLines);
        }

        public static string ReadAndFormatLuaFile(string luaFilePath)
        {
            if (!File.Exists(luaFilePath))
                return $"Lua file not found: {luaFilePath}";

            var lines = File.ReadAllLines(luaFilePath);
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
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
    }

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
