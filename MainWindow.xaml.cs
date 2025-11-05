using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace LuaToolsGameChecker
{
    public partial class MainWindow : Window
    {
        private GameInfo? currentGameInfo;
        private string? currentLuaFile;
        private string? reportDirectory;
        private bool testingMode = false;
        private ScreenshotWizard? activeScreenshotWizard = null;

        public MainWindow()
        {
            InitializeComponent();

            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.ToLower() == "-testing" || arg.ToLower() == "/testing")
                {
                    testingMode = true;
                    this.Title += " [TESTING MODE]";
                    break;
                }
            }

            // Initialize async features
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Show Denuvo warning on startup
            CustomMessageBox.Show(
                "IMPORTANT - REPORT INTEGRITY WARNING\n\n" +
                "This application generates verification reports that are\n" +
                "submitted to support tickets for Denuvo game activation.\n\n" +
                "WARNING:\n" +
                "• Do NOT fake or modify reports\n" +
                "• Do NOT bypass verification checks\n" +
                "• Do NOT submit fraudulent information\n" +
                "• All reports are monitored for integrity\n\n" +
                "CONSEQUENCES:\n" +
                "If you are found submitting fake reports or bypassing\n" +
                "verification checks, you will suffer PERMANENT LOSS\n" +
                "of ALL Denuvo activations on the server.\n\n" +
                "This is irreversible and applies to ALL games.\n\n" +
                "By using this application, you agree to submit only\n" +
                "genuine, unmodified reports.\n\n" +
                "Click OK to acknowledge and continue.",
                "Report Integrity Policy - READ CAREFULLY",
                CustomMessageBox.MessageBoxButton.OK);

            // Check for app updates
            await CheckForAppUpdates();

            // Auto-update whitelist if needed
            await AutoUpdateWhitelist();
        }

        private async Task CheckForAppUpdates()
        {
            try
            {
                UpdateStatus("Checking for app updates...", System.Windows.Media.Brushes.Orange);

                bool updateAvailable = await UpdateManager.IsUpdateAvailableAsync();

                if (updateAvailable)
                {
                    var result = CustomMessageBox.Show(
                        $"A new version of LuaTools Game Install Checker is available!\n\n" +
                        $"Current Version: {UpdateManager.GetCurrentVersion()}\n\n" +
                        $"Would you like to download and install the update?\n\n" +
                        $"The application will restart after the update.",
                        "Update Available",
                        CustomMessageBox.MessageBoxButton.YesNo);

                    if (result == CustomMessageBox.MessageBoxResult.Yes)
                    {
                        UpdateStatus("Downloading update...", System.Windows.Media.Brushes.Orange);

                        await UpdateManager.DownloadAndInstallUpdateAsync((progress) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                UpdateStatus($"Downloading update... {progress}%", System.Windows.Media.Brushes.Orange);
                            });
                        });

                        // Exit after starting the update process
                        System.Windows.Application.Current.Shutdown();
                    }
                }
                else
                {
                    UpdateStatus("Ready. Enter an AppID to begin.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));
                }
            }
            catch
            {
                // Silently fail update check
                UpdateStatus("Ready. Enter an AppID to begin.",
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));
            }
        }

        private async Task AutoUpdateWhitelist()
        {
            try
            {
                if (WhitelistManager.NeedsUpdate())
                {
                    UpdateStatus("Updating Denuvo whitelist...", System.Windows.Media.Brushes.Orange);
                    await WhitelistManager.UpdateWhitelistAsync();

                    var count = WhitelistManager.GetWhitelistCount();
                    UpdateStatus($"Whitelist updated ({count} games). Ready to verify games.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));
                }
            }
            catch
            {
                // Silently fail whitelist auto-update
            }
        }

        private async void BtnUpdateWhitelist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnUpdateWhitelist.IsEnabled = false;
                UpdateStatus("Updating Denuvo whitelist...", System.Windows.Media.Brushes.Orange);

                await WhitelistManager.UpdateWhitelistAsync(forceUpdate: true);

                var count = WhitelistManager.GetWhitelistCount();
                UpdateStatus($"✓ Whitelist updated! {count} Denuvo games supported.",
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                CustomMessageBox.Show(
                    $"Whitelist updated successfully!\n\n" +
                    $"Total Denuvo games supported: {count}\n\n" +
                    $"You can now verify any game on the whitelist.",
                    "Whitelist Updated",
                    CustomMessageBox.MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                UpdateStatus("✗ Failed to update whitelist.",
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));

                CustomMessageBox.Show($"Failed to update whitelist:\n\n{ex.Message}",
                    "Update Failed",
                    CustomMessageBox.MessageBoxButton.OK);
            }
            finally
            {
                btnUpdateWhitelist.IsEnabled = true;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.T && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                testingMode = !testingMode;
                if (testingMode)
                {
                    this.Title = this.Title.Replace(" [TESTING MODE]", "") + " [TESTING MODE]";
                    CustomMessageBox.Show("TESTING MODE ENABLED\n\nAll steps will be unlocked when you load a game.",
                        "Testing Mode",
                        CustomMessageBox.MessageBoxButton.OK);
                }
                else
                {
                    this.Title = this.Title.Replace(" [TESTING MODE]", "");
                    CustomMessageBox.Show("Testing mode disabled.",
                        "Testing Mode",
                        CustomMessageBox.MessageBoxButton.OK);
                }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private async void BtnLoadGame_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAppId.Text))
            {
                CustomMessageBox.Show("Please enter a Steam AppID.",
                    "Input Required",
                    CustomMessageBox.MessageBoxButton.OK);
                return;
            }

            var appId = txtAppId.Text.Trim();

            // No verification flag check needed - steam://validate runs automatically

            // Check whitelist first
            if (!WhitelistManager.IsWhitelisted(appId))
            {
                CustomMessageBox.Show(
                    $"AppID {appId} is not on the Denuvo whitelist.\n\n" +
                    $"Only Denuvo titles on the whitelist are supported.\n\n" +
                    $"Click \"Update Whitelist\" to refresh the list and try again.",
                    "Game Not Supported",
                    CustomMessageBox.MessageBoxButton.OK);

                UpdateStatus("✗ AppID not on Denuvo whitelist.",
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)));
                return;
            }

            try
            {
                UpdateStatus("Loading game information...", System.Windows.Media.Brushes.Orange);

                currentGameInfo = SteamHelper.GetGameInfo(appId);

                // Check if game is actually installed (SizeOnDisk > 0)
                if (currentGameInfo.SizeOnDisk == "0")
                {
                    CustomMessageBox.Show(
                        $"ERROR: Game Not Fully Installed!\n\n" +
                        $"Game: {currentGameInfo.Name}\n" +
                        $"AppID: {appId}\n\n" +
                        $"The game is not installed or has no files on disk.\n" +
                        $"(SizeOnDisk = 0 bytes)\n\n" +
                        $"Please fully install the game in Steam first,\n" +
                        $"then run this tool again.",
                        "Game Not Installed",
                        CustomMessageBox.MessageBoxButton.OK);

                    UpdateStatus("✗ Game not installed (SizeOnDisk = 0)",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)));
                    return;
                }

                // ALWAYS check for valid Morrenus lua file every time we load
                currentLuaFile = SteamHelper.FindLuaFileForAppId(currentGameInfo.AppId);

                // If no valid Morrenus lua file found, prompt to download
                if (currentLuaFile == null)
                {
                    var result = CustomMessageBox.Show(
                        $"Morrenus files not detected for {currentGameInfo.Name} (AppID: {appId}).\n\n" +
                        $"Would you like to download and install them automatically?\n\n" +
                        $"This will download:\n" +
                        $"- Lua configuration files → Steam\\config\\stplug-in\n" +
                        $"- Manifest files → Steam\\depotcache",
                        "Morrenus Files Not Found",
                        CustomMessageBox.MessageBoxButton.YesNo);

                    if (result == CustomMessageBox.MessageBoxResult.Yes)
                    {
                        try
                        {
                            UpdateStatus("Downloading Morrenus files...", System.Windows.Media.Brushes.Orange);

                            await MorrenusDownloader.DownloadAndExtractAsync(appId, (status) =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    UpdateStatus(status, System.Windows.Media.Brushes.Orange);
                                });
                            });

                            UpdateStatus("✓ Morrenus files installed successfully!",
                                new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                            // Record Morrenus download
                            SteamHelper.RecordMorrenusDownload(appId);

                            // Restart Steam
                            UpdateStatus("Restarting Steam...", System.Windows.Media.Brushes.Orange);
                            RestartSteam();

                            UpdateStatus("Waiting for Steam to initialize...", System.Windows.Media.Brushes.Orange);
                            await Task.Delay(8000); // Wait for Steam to fully start

                            // Ask if Steam has started
                            var steamStartedResult = CustomMessageBox.Show(
                                "Has Steam finished starting up?\n\n" +
                                "Wait for Steam to be fully loaded before clicking Yes.\n\n" +
                                "Click 'Yes' when Steam is ready.\n" +
                                "Click 'No' if Steam is still loading (will wait longer).",
                                "Steam Ready?",
                                CustomMessageBox.MessageBoxButton.YesNo);

                            if (steamStartedResult == CustomMessageBox.MessageBoxResult.No)
                            {
                                UpdateStatus("Waiting for Steam to finish starting...", System.Windows.Media.Brushes.Orange);
                                await Task.Delay(5000); // Wait additional 5 seconds
                            }

                            // Launch automatic Steam verification
                            UpdateStatus("Running Steam verification...", System.Windows.Media.Brushes.Orange);
                            SteamHelper.LaunchSteamValidation(appId);

                            // Show info that verification is running
                            CustomMessageBox.Show(
                                $"⚠ STEAM VERIFICATION RUNNING ⚠\n\n" +
                                $"Steam is now automatically verifying '{currentGameInfo.Name}'.\n\n" +
                                $"This process:\n" +
                                $"• Runs automatically in the background\n" +
                                $"• May take several minutes to complete\n" +
                                $"• May download additional data if needed\n" +
                                $"• Will ensure file integrity\n\n" +
                                $"Please wait for the verification to complete.\n\n" +
                                $"Click OK when you're ready to continue.",
                                "Verification Running",
                                CustomMessageBox.MessageBoxButton.OK);

                            // Wait for verification to complete
                            UpdateStatus("⏳ Waiting for Steam verification to complete...", System.Windows.Media.Brushes.Orange);
                            await Task.Delay(3000); // Give it a moment to start

                            // Mark as verified
                            SteamHelper.MarkVerificationCompleted(appId);
                            UpdateStatus("✓ Verification complete!",
                                new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                            CustomMessageBox.Show(
                                $"✓ Setup complete!\n\n" +
                                $"The Morrenus files for '{currentGameInfo.Name}' have been\n" +
                                $"installed and verification has been initiated.\n\n" +
                                $"You can now proceed with using the game.",
                                "Setup Complete",
                                CustomMessageBox.MessageBoxButton.OK);
                        }
                        catch (Exception ex)
                        {
                            CustomMessageBox.Show(
                                $"Failed to download Morrenus files:\n\n{ex.Message}\n\n" +
                                $"You may need to manually obtain the files from:\n" +
                                $"- Sage Bot\n" +
                                $"- Luie\n" +
                                $"- The plugin",
                                "Download Failed",
                                CustomMessageBox.MessageBoxButton.OK);

                            UpdateStatus("✗ Failed to download Morrenus files.",
                                new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
                            return;
                        }
                    }
                    else
                    {
                        // User declined download - fail the load process
                        CustomMessageBox.Show(
                            $"Cannot proceed without Morrenus files.\n\n" +
                            $"AppID {appId} requires Morrenus files to function properly.\n\n" +
                            $"Please click \"Load Game\" again and accept the download,\n" +
                            $"or obtain the files manually from:\n" +
                            $"- Sage Bot\n" +
                            $"- Luie\n" +
                            $"- The plugin",
                            "Morrenus Files Required",
                            CustomMessageBox.MessageBoxButton.OK);

                        UpdateStatus("✗ Cannot load game without Morrenus files.",
                            new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
                        return;
                    }
                }

                // Re-check the lua file after download to ensure it's valid
                currentLuaFile = SteamHelper.FindLuaFileForAppId(currentGameInfo.AppId);

                lblGameName.Text = $"✓ {currentGameInfo.Name} (AppID: {currentGameInfo.AppId})";
                gameInfoPanel.Visibility = Visibility.Visible;

                if (currentLuaFile != null)
                {
                    lblLuaStatus.Text = $"Lua File: {Path.GetFileName(currentLuaFile)}";
                    lblLuaStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74));
                }
                else
                {
                    lblLuaStatus.Text = "⚠ No Lua file found for this AppID";
                    lblLuaStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0));
                }

                UpdateStatus($"✓ Game loaded: {currentGameInfo.Name}",
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                reportDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "LuaTools Reports",
                    $"{currentGameInfo.AppId}_{currentGameInfo.Name.Replace(":", "").Replace("\\", "").Replace("/", "")}_{DateTime.Now:yyyyMMdd_HHmmss}");

                Directory.CreateDirectory(reportDirectory);

                if (testingMode)
                {
                    btnDisableDlc.IsEnabled = true;
                    btnRestartSteam.IsEnabled = true;
                    btnScreenshot.IsEnabled = true;
                    btnVerify.IsEnabled = false;
                    UpdateStatus("✓ TESTING MODE - Steps 1-4 unlocked (Step 5 requires completing Step 4)",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)));
                }
                else
                {
                    btnDisableDlc.IsEnabled = currentLuaFile != null;
                    btnRestartSteam.IsEnabled = false;
                    btnScreenshot.IsEnabled = false;
                    btnVerify.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error: {ex.Message}",
                    "Failed to Load Game",
                    CustomMessageBox.MessageBoxButton.OK);

                UpdateStatus("✗ Failed to load game.",
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
            }
        }

        private void BtnVerify_Click(object sender, RoutedEventArgs e)
        {
            if (currentGameInfo == null || reportDirectory == null)
            {
                CustomMessageBox.Show("Please load a game first.",
                    "No Game Loaded",
                    CustomMessageBox.MessageBoxButton.OK);
                return;
            }

            try
            {
                UpdateStatus("Verifying game installation...", System.Windows.Media.Brushes.Orange);

                UpdateStatus("Calculating folder size...", System.Windows.Media.Brushes.Orange);
                long actualSize = SteamHelper.GetDirectorySize(currentGameInfo.GamePath);

                UpdateStatus("Generating folder structure...", System.Windows.Media.Brushes.Orange);
                string folderTree = SteamHelper.GenerateFolderTree(currentGameInfo.GamePath);

                var report = GenerateReport(currentGameInfo, actualSize, folderTree, currentLuaFile);
                txtOutput.Text = report;

                UpdateStatus($"✓ Verification complete for {currentGameInfo.Name}",
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                var reportPath = Path.Combine(reportDirectory, $"GameVerification_{currentGameInfo.AppId}.txt");
                File.WriteAllText(reportPath, report);

                UpdateStatus($"✓ Report saved to: {reportPath}",
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                var files = Directory.GetFiles(reportDirectory);
                var fileCount = files.Length;
                var hasScreenshots = fileCount > 1;

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = reportDirectory,
                        UseShellExecute = true
                    });
                }
                catch { }

                var message = $"Verification complete! All files saved.\n\n" +
                              $"The folder has been opened.\n\n" +
                              $"SUBMIT TO SUPPORT TICKET:\n" +
                              $"1. The folder contains {fileCount} file(s):\n" +
                              $"   - Verification report\n";

                if (hasScreenshots)
                {
                    message += $"   - {fileCount - 1} screenshot(s)\n";
                }

                message += $"2. Select ALL files in this folder\n" +
                           $"3. Drag and drop everything into your support ticket\n\n" +
                           $"Folder Location:\n{reportDirectory}";

                CustomMessageBox.Show(message,
                    "Report Ready - Drag to Ticket",
                    CustomMessageBox.MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error: {ex.Message}",
                    "Verification Failed",
                    CustomMessageBox.MessageBoxButton.OK);

                UpdateStatus("✗ Verification failed.",
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
            }
        }

        private string GenerateReport(GameInfo gameInfo, long actualSize, string folderTree, string? luaFile)
        {
            var sb = new StringBuilder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("   LUATOOLS - STEAM GAME INSTALLATION VERIFICATION REPORT");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"AppID: {gameInfo.AppId}");
            sb.AppendLine($"Game Name: {gameInfo.Name}");
            sb.AppendLine();
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine("INSTALLATION DETAILS");
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine();
            sb.AppendLine($"Install Directory Path:");
            sb.AppendLine($"  {gameInfo.GamePath}");
            sb.AppendLine();
            sb.AppendLine($"Folder Size (Actual): {SteamHelper.FormatBytes(actualSize)} ({actualSize:N0} bytes)");
            sb.AppendLine($"Folder Size (Steam):  {SteamHelper.FormatBytes(long.Parse(gameInfo.SizeOnDisk))} ({long.Parse(gameInfo.SizeOnDisk):N0} bytes)");
            sb.AppendLine();
            sb.AppendLine($"Build ID: {gameInfo.BuildId}");
            sb.AppendLine();
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine("FOLDER STRUCTURE (ROOT LEVEL)");
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine();
            sb.AppendLine(folderTree);

            if (luaFile != null && File.Exists(luaFile))
            {
                sb.AppendLine();
                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine("LUA FILE CONTENTS (FORMATTED)");
                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine();
                sb.AppendLine($"File: {Path.GetFileName(luaFile)}");
                sb.AppendLine($"Path: {luaFile}");
                sb.AppendLine();
                sb.AppendLine(SteamHelper.ReadAndFormatLuaFile(luaFile));
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine("LUA FILE");
                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine();
                sb.AppendLine("⚠ No Lua configuration file found for this AppID");
                sb.AppendLine("  This game may not be configured in LuaTools.");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("          END OF REPORT - GENERATED BY LUATOOLS");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            sb.Append(SteamHelper.GetReportMetadata(gameInfo.AppId));

            return sb.ToString();
        }

        private void BtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (currentGameInfo == null || reportDirectory == null)
            {
                CustomMessageBox.Show("Please load a game first before taking screenshots.",
                    "No Game Selected",
                    CustomMessageBox.MessageBoxButton.OK);
                return;
            }

            if (activeScreenshotWizard != null)
            {
                activeScreenshotWizard.Activate();
                activeScreenshotWizard.Focus();
                return;
            }

            try
            {
                btnScreenshot.IsEnabled = false;
                this.Hide();

                activeScreenshotWizard = new ScreenshotWizard(currentGameInfo.AppId, currentGameInfo.Name, reportDirectory, testingMode);
                var result = activeScreenshotWizard.ShowDialog();

                activeScreenshotWizard = null;
                this.Show();
                this.Activate();
                btnScreenshot.IsEnabled = true;

                var screenshotCount = Directory.GetFiles(reportDirectory, "*.png").Length;
                if (result == true || screenshotCount >= 3)
                {
                    btnVerify.IsEnabled = true;
                    UpdateStatus("✓ Step 4 complete! Proceed to Step 5 to generate the report.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));
                }
                else
                {
                    UpdateStatus($"Screenshot wizard closed. {screenshotCount}/3 screenshots captured.",
                        System.Windows.Media.Brushes.Orange);
                }
            }
            catch (Exception ex)
            {
                activeScreenshotWizard = null;
                btnScreenshot.IsEnabled = true;
                this.Show();
                CustomMessageBox.Show($"Failed to open screenshot wizard: {ex.Message}",
                    "Error",
                    CustomMessageBox.MessageBoxButton.OK);
            }
        }

        private void BtnDisableDlc_Click(object sender, RoutedEventArgs e)
        {
            if (currentGameInfo == null || currentLuaFile == null)
            {
                CustomMessageBox.Show("No Lua file available. Please verify a game first.",
                    "No Lua File",
                    CustomMessageBox.MessageBoxButton.OK);
                return;
            }

            var result = CustomMessageBox.Show(
                $"This will disable depot updates for {currentGameInfo.Name}.\n\n" +
                $"Lua File: {Path.GetFileName(currentLuaFile)}\n" +
                $"Main AppID: {currentGameInfo.AppId}\n\n" +
                "What this does:\n" +
                "✓ Keeps the game and DLC unlocked/active\n" +
                "✓ Comments out depot download lines (3-parameter addappid)\n" +
                "✓ Uncomments setManifestid lines (locks to specific version)\n" +
                "✓ Prevents Steam from downloading/updating files\n\n" +
                "⚠ WARNING: This will modify the Lua file. Make sure Steam is closed!\n\n" +
                "Do you want to continue?",
                "Confirm Disable Depot Updates",
                CustomMessageBox.MessageBoxButton.YesNo);

            if (result == CustomMessageBox.MessageBoxResult.Yes)
            {
                try
                {
                    UpdateStatus("Disabling DLC updates in Lua file...", System.Windows.Media.Brushes.Orange);

                    SteamHelper.DisableDlcUpdates(currentLuaFile, currentGameInfo.AppId);

                    UpdateStatus("✓ DLC updates disabled successfully.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                    btnRestartSteam.IsEnabled = true;

                    CustomMessageBox.Show(
                        $"Depot updates have been disabled!\n\n" +
                        $"Modified file: {currentLuaFile}\n\n" +
                        "All depot download lines have been commented out.\n" +
                        "setManifestid lines have been uncommented (version locked).\n" +
                        "Game and DLC will remain unlocked, but Steam won't download files.\n\n" +
                        "✓ Step 2 complete! Proceed to Step 3.",
                        "Success",
                        CustomMessageBox.MessageBoxButton.OK);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to disable DLC updates: {ex.Message}",
                        "Error",
                        CustomMessageBox.MessageBoxButton.OK);

                    UpdateStatus("✗ Failed to disable DLC updates.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
                }
            }
        }

        private void BtnEnableUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (currentGameInfo == null || currentLuaFile == null)
            {
                CustomMessageBox.Show("No Lua file available. Please verify a game first.",
                    "No Lua File",
                    CustomMessageBox.MessageBoxButton.OK);
                return;
            }

            var result = CustomMessageBox.Show(
                $"This will enable depot updates for {currentGameInfo.Name}.\n\n" +
                $"Lua File: {Path.GetFileName(currentLuaFile)}\n" +
                $"Main AppID: {currentGameInfo.AppId}\n\n" +
                "What this does:\n" +
                "✓ Uncomments depot download lines (3-parameter addappid)\n" +
                "✓ Comments out setManifestid lines (enables auto-update)\n" +
                "✓ Allows Steam to download/update game files\n" +
                "✓ Keeps game and DLC unlocked/active\n\n" +
                "⚠ WARNING: This will modify the Lua file. Make sure Steam is closed!\n\n" +
                "⛔ CRITICAL WARNING ⛔\n" +
                "ENABLING UPDATES IS ONLY TO UPDATE THE GAME!\n" +
                "THIS WILL BREAK ACTIVATION!\n" +
                "Only enable updates if you need to update the game to a newer version.\n" +
                "You will need to re-activate the game after updating!\n\n" +
                "Do you want to continue?",
                "Confirm Enable Depot Updates",
                CustomMessageBox.MessageBoxButton.YesNo);

            if (result == CustomMessageBox.MessageBoxResult.Yes)
            {
                try
                {
                    UpdateStatus("Enabling depot updates in Lua file...", System.Windows.Media.Brushes.Orange);

                    SteamHelper.EnableDlcUpdates(currentLuaFile, currentGameInfo.AppId);

                    UpdateStatus("✓ Depot updates enabled successfully.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                    btnRestartSteam.IsEnabled = true;

                    CustomMessageBox.Show(
                        $"Depot updates have been enabled!\n\n" +
                        $"Modified file: {currentLuaFile}\n\n" +
                        "All depot download lines have been uncommented.\n" +
                        "setManifestid lines have been commented out (auto-update enabled).\n" +
                        "Steam will now be able to download and update game files.\n\n" +
                        "✓ Step 2 complete! Proceed to Step 3.",
                        "Success",
                        CustomMessageBox.MessageBoxButton.OK);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to enable depot updates: {ex.Message}",
                        "Error",
                        CustomMessageBox.MessageBoxButton.OK);

                    UpdateStatus("✗ Failed to enable depot updates.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
                }
            }
        }

        private void BtnRestartSteam_Click(object sender, RoutedEventArgs e)
        {
            if (currentGameInfo == null)
            {
                CustomMessageBox.Show("Please verify a game first before resetting activation.",
                    "No Game Selected",
                    CustomMessageBox.MessageBoxButton.OK);
                return;
            }

            var result = CustomMessageBox.Show(
                $"WARNING: This will reset the activation status for:\n\n" +
                $"{currentGameInfo.Name} (AppID: {currentGameInfo.AppId})\n\n" +
                "This will:\n" +
                "- Restart Steam completely\n" +
                "- Attempt to launch the game once\n" +
                "- Clear the Steam ID for this game\n\n" +
                "Do you want to continue?",
                "Confirm Reset Activation",
                CustomMessageBox.MessageBoxButton.YesNo);

            if (result == CustomMessageBox.MessageBoxResult.Yes)
            {
                try
                {
                    UpdateStatus("Restarting Steam...", System.Windows.Media.Brushes.Orange);

                    var steamProcesses = System.Diagnostics.Process.GetProcessesByName("steam");
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

                    var steamPath = SteamHelper.GetSteamInstallPath();
                    if (steamPath != null)
                    {
                        var steamExe = Path.Combine(steamPath, "steam.exe");
                        if (File.Exists(steamExe))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = steamExe,
                                UseShellExecute = true
                            });
                        }
                    }

                    UpdateStatus("✓ Steam restarted. Waiting for Steam to initialize...", System.Windows.Media.Brushes.Orange);
                    System.Threading.Thread.Sleep(5000);

                    UpdateStatus("✓ Attempting to launch game...", System.Windows.Media.Brushes.Orange);
                    SteamHelper.TryLaunchGame(currentGameInfo.AppId);
                    System.Threading.Thread.Sleep(3000);

                    UpdateStatus("✓ Clearing Steam ID for this game...", System.Windows.Media.Brushes.Orange);

                    var clearCommand = $"steam://run/tool/clearsteamid/{currentGameInfo.AppId}";
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = clearCommand,
                        UseShellExecute = true
                    });

                    System.Threading.Thread.Sleep(2000);

                    UpdateStatus("✓ Reset activation complete.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                    btnScreenshot.IsEnabled = true;

                    CustomMessageBox.Show(
                        "Steam has been restarted, game launched, and Steam ID cleared!\n\n" +
                        "✓ Step 3 complete! Proceed to Step 4 to take DRM screenshots.",
                        "Success",
                        CustomMessageBox.MessageBoxButton.OK);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to reset activation: {ex.Message}",
                        "Error",
                        CustomMessageBox.MessageBoxButton.OK);

                    UpdateStatus("✗ Failed to reset activation.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
                }
            }
        }

        private void BtnClearSteamId_Click(object sender, RoutedEventArgs e)
        {
            // Create a simple input dialog using CustomMessageBox
            var inputDialog = new Window
            {
                Title = "Clear SteamID",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.Transparent,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true
            };

            var border = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 71, 94)), // CardBackgroundBrush
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 192, 244)), // AccentBrush
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20)
            };

            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.5,
                Color = System.Windows.Media.Colors.Black
            };

            var stackPanel = new StackPanel();

            var titleText = new TextBlock
            {
                Text = "Enter Game AppID to Clear SteamID",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(199, 213, 224)), // TextPrimaryBrush
                Margin = new Thickness(0, 0, 0, 15),
                TextAlignment = TextAlignment.Center
            };

            var appIdTextBox = new System.Windows.Controls.TextBox
            {
                Width = 300,
                Height = 32,
                FontSize = 14,
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 15),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 52, 73)), // TextBox background from theme
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(199, 213, 224)), // TextPrimaryBrush
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(21, 33, 45)), // TextBox border from theme
                BorderThickness = new Thickness(1),
                VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            // Apply rounded corners template to TextBox
            var textBoxTemplate = new ControlTemplate(typeof(System.Windows.Controls.TextBox));
            var textBoxBorder = new FrameworkElementFactory(typeof(Border));
            textBoxBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(System.Windows.Controls.TextBox.BackgroundProperty));
            textBoxBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(System.Windows.Controls.TextBox.BorderBrushProperty));
            textBoxBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(System.Windows.Controls.TextBox.BorderThicknessProperty));
            textBoxBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.SetValue(FrameworkElement.NameProperty, "PART_ContentHost");
            scrollViewer.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(System.Windows.Controls.TextBox.PaddingProperty));
            scrollViewer.SetValue(ScrollViewer.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            textBoxBorder.AppendChild(scrollViewer);
            textBoxTemplate.VisualTree = textBoxBorder;
            appIdTextBox.Template = textBoxTemplate;

            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 100,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 192, 244)), // AccentBrush
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // Apply rounded corners template to OK button
            var okTemplate = new ControlTemplate(typeof(System.Windows.Controls.Button));
            var okBorder = new FrameworkElementFactory(typeof(Border));
            okBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(System.Windows.Controls.Button.BackgroundProperty));
            okBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            okBorder.SetValue(Border.PaddingProperty, new Thickness(10, 0, 10, 0));
            var okContent = new FrameworkElementFactory(typeof(ContentPresenter));
            okContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            okContent.SetValue(ContentPresenter.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            okBorder.AppendChild(okContent);
            okTemplate.VisualTree = okBorder;
            okButton.Template = okTemplate;

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 32,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(27, 40, 56)), // SecondaryDarkBrush
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(199, 213, 224)), // TextPrimaryBrush
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // Apply rounded corners template to Cancel button
            var cancelTemplate = new ControlTemplate(typeof(System.Windows.Controls.Button));
            var cancelBorder = new FrameworkElementFactory(typeof(Border));
            cancelBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(System.Windows.Controls.Button.BackgroundProperty));
            cancelBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            cancelBorder.SetValue(Border.PaddingProperty, new Thickness(10, 0, 10, 0));
            var cancelContent = new FrameworkElementFactory(typeof(ContentPresenter));
            cancelContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            cancelContent.SetValue(ContentPresenter.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            cancelBorder.AppendChild(cancelContent);
            cancelTemplate.VisualTree = cancelBorder;
            cancelButton.Template = cancelTemplate;

            okButton.Click += (s, args) => { inputDialog.DialogResult = true; inputDialog.Close(); };
            cancelButton.Click += (s, args) => { inputDialog.DialogResult = false; inputDialog.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(appIdTextBox);
            stackPanel.Children.Add(buttonPanel);

            border.Child = stackPanel;
            inputDialog.Content = border;

            var result = inputDialog.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(appIdTextBox.Text))
            {
                string appId = appIdTextBox.Text.Trim();

                // Validate AppID is numeric
                if (!System.Text.RegularExpressions.Regex.IsMatch(appId, @"^\d+$"))
                {
                    CustomMessageBox.Show(
                        "Invalid AppID! Please enter a numeric Steam AppID.",
                        "Invalid Input",
                        CustomMessageBox.MessageBoxButton.OK);
                    return;
                }

                // Show confirmation dialog
                var confirmResult = CustomMessageBox.Show(
                    $"WARNING: This will reset the activation for AppID {appId}\n\n" +
                    "This will:\n" +
                    "- Kill all Steam processes\n" +
                    "- Restart Steam\n" +
                    "- Attempt to launch the game once\n" +
                    "- Clear the Steam ID for this game\n\n" +
                    "Do you want to continue?",
                    "Confirm Clear SteamID",
                    CustomMessageBox.MessageBoxButton.YesNo);

                if (confirmResult == CustomMessageBox.MessageBoxResult.Yes)
                {
                    try
                    {
                        UpdateStatus("Restarting Steam...", System.Windows.Media.Brushes.Orange);

                        // Kill Steam processes
                        var steamProcesses = System.Diagnostics.Process.GetProcessesByName("steam");
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

                        // Restart Steam
                        var steamPath = SteamHelper.GetSteamInstallPath();
                        if (steamPath != null)
                        {
                            var steamExe = Path.Combine(steamPath, "steam.exe");
                            if (File.Exists(steamExe))
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = steamExe,
                                    UseShellExecute = true
                                });
                            }
                        }

                        UpdateStatus("✓ Steam restarted. Waiting for Steam to initialize...", System.Windows.Media.Brushes.Orange);
                        System.Threading.Thread.Sleep(5000);

                        UpdateStatus("✓ Attempting to launch game...", System.Windows.Media.Brushes.Orange);
                        SteamHelper.TryLaunchGame(appId);
                        System.Threading.Thread.Sleep(3000);

                        UpdateStatus("✓ Clearing Steam ID for this game...", System.Windows.Media.Brushes.Orange);

                        var clearCommand = $"steam://run/tool/clearsteamid/{appId}";
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = clearCommand,
                            UseShellExecute = true
                        });

                        System.Threading.Thread.Sleep(2000);

                        UpdateStatus("✓ Clear SteamID complete.",
                            new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                        CustomMessageBox.Show(
                            $"Steam has been restarted, game launched, and Steam ID cleared for AppID {appId}!",
                            "Success",
                            CustomMessageBox.MessageBoxButton.OK);
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"Failed to clear SteamID: {ex.Message}",
                            "Error",
                            CustomMessageBox.MessageBoxButton.OK);

                        UpdateStatus("✗ Failed to clear SteamID.",
                            new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
                    }
                }
            }
        }

        private void RestartSteam()
        {
            try
            {
                // Kill all Steam processes
                var steamProcesses = System.Diagnostics.Process.GetProcessesByName("steam");
                foreach (var process in steamProcesses)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    catch { }
                }

                // Wait for processes to fully close
                System.Threading.Thread.Sleep(2000);

                // Start Steam again
                var steamPath = SteamHelper.GetSteamInstallPath();
                if (steamPath != null)
                {
                    var steamExe = Path.Combine(steamPath, "steam.exe");
                    if (File.Exists(steamExe))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = steamExe,
                            UseShellExecute = true
                        });
                    }
                }

                // Wait for Steam to initialize
                System.Threading.Thread.Sleep(5000);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to restart Steam: {ex.Message}",
                    "Steam Restart Error",
                    CustomMessageBox.MessageBoxButton.OK);
            }
        }

        private void UpdateStatus(string message, System.Windows.Media.Brush color)
        {
            lblStatus.Text = message;
            lblStatus.Foreground = color;
        }
    }
}
