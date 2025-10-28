using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LuaToolsGameChecker
{
    public class WhitelistManager
    {
        private static readonly string WhitelistDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "LuaTools Reports");

        private static readonly string WhitelistFilePath = Path.Combine(WhitelistDirectory, "denuvo_whitelist.txt");

        private static readonly string PrimaryUrl = "https://raw.githubusercontent.com/madoiscool/lt_api_links/refs/heads/main/denuvoappids";
        private static readonly string FallbackUrl = "https://luatools.vercel.app/denuvoappids";

        private static HashSet<string> cachedWhitelist = new HashSet<string>();

        /// <summary>
        /// Checks if the whitelist needs updating (doesn't exist or is older than 7 days)
        /// </summary>
        public static bool NeedsUpdate()
        {
            if (!File.Exists(WhitelistFilePath))
                return true;

            var fileInfo = new FileInfo(WhitelistFilePath);
            var age = DateTime.Now - fileInfo.LastWriteTime;
            return age.TotalDays >= 7;
        }

        /// <summary>
        /// Downloads the whitelist from the remote source with fallback
        /// </summary>
        public static async Task<bool> UpdateWhitelistAsync(bool forceUpdate = false)
        {
            try
            {
                if (!forceUpdate && !NeedsUpdate())
                    return true;

                // Ensure directory exists
                Directory.CreateDirectory(WhitelistDirectory);

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                string content = null;

                try
                {
                    // Try primary URL
                    var response = await client.GetAsync(PrimaryUrl);
                    response.EnsureSuccessStatusCode();
                    content = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("429"))
                {
                    // GitHub rate limited, try fallback
                    try
                    {
                        var response = await client.GetAsync(FallbackUrl);
                        response.EnsureSuccessStatusCode();
                        content = await response.Content.ReadAsStringAsync();
                    }
                    catch
                    {
                        throw new Exception("Both primary and fallback URLs failed. GitHub rate limited and fallback unavailable.");
                    }
                }

                if (string.IsNullOrWhiteSpace(content))
                    throw new Exception("Downloaded whitelist is empty");

                // Write to file
                await File.WriteAllTextAsync(WhitelistFilePath, content);

                // Update cache
                LoadWhitelistToCache();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update whitelist: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the whitelist from disk into memory cache
        /// </summary>
        public static void LoadWhitelistToCache()
        {
            try
            {
                if (!File.Exists(WhitelistFilePath))
                {
                    cachedWhitelist.Clear();
                    return;
                }

                var lines = File.ReadAllLines(WhitelistFilePath);
                cachedWhitelist = new HashSet<string>(
                    lines.Where(line => !string.IsNullOrWhiteSpace(line))
                         .Select(line => line.Trim())
                );
            }
            catch
            {
                cachedWhitelist.Clear();
            }
        }

        /// <summary>
        /// Checks if an AppID is in the whitelist
        /// </summary>
        public static bool IsWhitelisted(string appId)
        {
            if (cachedWhitelist.Count == 0)
                LoadWhitelistToCache();

            return cachedWhitelist.Contains(appId);
        }

        /// <summary>
        /// Gets the count of whitelisted AppIDs
        /// </summary>
        public static int GetWhitelistCount()
        {
            if (cachedWhitelist.Count == 0)
                LoadWhitelistToCache();

            return cachedWhitelist.Count;
        }
    }
}
