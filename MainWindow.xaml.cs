using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

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
                    System.Windows.MessageBox.Show("TESTING MODE ENABLED\n\nAll steps will be unlocked when you load a game.",
                        "Testing Mode",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    this.Title = this.Title.Replace(" [TESTING MODE]", "");
                    System.Windows.MessageBox.Show("Testing mode disabled.",
                        "Testing Mode",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
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


        private void BtnLoadGame_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAppId.Text))
            {
                System.Windows.MessageBox.Show("Please enter a Steam AppID.",
                    "Input Required",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                UpdateStatus("Loading game information...", System.Windows.Media.Brushes.Orange);

                currentGameInfo = SteamHelper.GetGameInfo(txtAppId.Text.Trim());
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
                    btnEnableUpdates.IsEnabled = true;
                    btnRestartSteam.IsEnabled = true;
                    btnScreenshot.IsEnabled = true;
                    btnVerify.IsEnabled = false;
                    UpdateStatus("✓ TESTING MODE - Steps 1-4 unlocked (Step 5 requires completing Step 4)",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)));
                }
                else
                {
                    btnDisableDlc.IsEnabled = currentLuaFile != null;
                    btnEnableUpdates.IsEnabled = currentLuaFile != null;
                    btnRestartSteam.IsEnabled = false;
                    btnScreenshot.IsEnabled = false;
                    btnVerify.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}",
                    "Failed to Load Game",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);

                UpdateStatus("✗ Failed to load game.",
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
            }
        }

        private void BtnVerify_Click(object sender, RoutedEventArgs e)
        {
            if (currentGameInfo == null || reportDirectory == null)
            {
                System.Windows.MessageBox.Show("Please load a game first.",
                    "No Game Loaded",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
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

                System.Windows.MessageBox.Show(message,
                    "Report Ready - Drag to Ticket",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}",
                    "Verification Failed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);

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

            return sb.ToString();
        }

        private void BtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (currentGameInfo == null || reportDirectory == null)
            {
                System.Windows.MessageBox.Show("Please load a game first before taking screenshots.",
                    "No Game Selected",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
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
                System.Windows.MessageBox.Show($"Failed to open screenshot wizard: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnDisableDlc_Click(object sender, RoutedEventArgs e)
        {
            if (currentGameInfo == null || currentLuaFile == null)
            {
                System.Windows.MessageBox.Show("No Lua file available. Please verify a game first.",
                    "No Lua File",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
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
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    UpdateStatus("Disabling DLC updates in Lua file...", System.Windows.Media.Brushes.Orange);

                    SteamHelper.DisableDlcUpdates(currentLuaFile, currentGameInfo.AppId);

                    UpdateStatus("✓ DLC updates disabled successfully.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                    btnRestartSteam.IsEnabled = true;

                    System.Windows.MessageBox.Show(
                        $"Depot updates have been disabled!\n\n" +
                        $"Modified file: {currentLuaFile}\n\n" +
                        "All depot download lines have been commented out.\n" +
                        "setManifestid lines have been uncommented (version locked).\n" +
                        "Game and DLC will remain unlocked, but Steam won't download files.\n\n" +
                        "✓ Step 2 complete! Proceed to Step 3.",
                        "Success",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to disable DLC updates: {ex.Message}",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);

                    UpdateStatus("✗ Failed to disable DLC updates.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
                }
            }
        }

        private void BtnEnableUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (currentGameInfo == null || currentLuaFile == null)
            {
                System.Windows.MessageBox.Show("No Lua file available. Please verify a game first.",
                    "No Lua File",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
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
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    UpdateStatus("Enabling depot updates in Lua file...", System.Windows.Media.Brushes.Orange);

                    SteamHelper.EnableDlcUpdates(currentLuaFile, currentGameInfo.AppId);

                    UpdateStatus("✓ Depot updates enabled successfully.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)));

                    btnRestartSteam.IsEnabled = true;

                    System.Windows.MessageBox.Show(
                        $"Depot updates have been enabled!\n\n" +
                        $"Modified file: {currentLuaFile}\n\n" +
                        "All depot download lines have been uncommented.\n" +
                        "setManifestid lines have been commented out (auto-update enabled).\n" +
                        "Steam will now be able to download and update game files.\n\n" +
                        "✓ Step 2 complete! Proceed to Step 3.",
                        "Success",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to enable depot updates: {ex.Message}",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);

                    UpdateStatus("✗ Failed to enable depot updates.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
                }
            }
        }

        private void BtnRestartSteam_Click(object sender, RoutedEventArgs e)
        {
            if (currentGameInfo == null)
            {
                System.Windows.MessageBox.Show("Please verify a game first before resetting activation.",
                    "No Game Selected",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"WARNING: This will reset the activation status for:\n\n" +
                $"{currentGameInfo.Name} (AppID: {currentGameInfo.AppId})\n\n" +
                "This will:\n" +
                "- Restart Steam completely\n" +
                "- Attempt to launch the game once\n" +
                "- Clear the Steam ID for this game\n\n" +
                "Do you want to continue?",
                "Confirm Reset Activation",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
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

                    System.Windows.MessageBox.Show(
                        "Steam has been restarted, game launched, and Steam ID cleared!\n\n" +
                        "✓ Step 3 complete! Proceed to Step 4 to take DRM screenshots.",
                        "Success",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to reset activation: {ex.Message}",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);

                    UpdateStatus("✗ Failed to reset activation.",
                        new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 87, 34)));
                }
            }
        }

        private void UpdateStatus(string message, System.Windows.Media.Brush color)
        {
            lblStatus.Text = message;
            lblStatus.Foreground = color;
        }
    }
}
