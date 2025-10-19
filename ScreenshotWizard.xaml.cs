using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WinFormsScreen = System.Windows.Forms.Screen;

namespace LuaToolsGameChecker
{
    public partial class ScreenshotWizard : Window
    {
        private string screenshotDirectory;
        private string? screenshot1Path;
        private string? screenshot2Path;
        private string? screenshot3Path;
        private bool testingMode;
        private bool isCapturing = false;

        public ScreenshotWizard(string appId, string gameName, string reportDirectory, bool testingMode = false)
        {
            InitializeComponent();

            // Use the same directory as the report
            screenshotDirectory = reportDirectory;
            this.testingMode = testingMode;

            if (testingMode)
            {
                this.Title = "Screenshot Wizard [TESTING MODE]";
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnCapture1_Click(object sender, RoutedEventArgs e)
        {
            if (isCapturing) return;
            btnCapture1.IsEnabled = false;
            this.WindowState = WindowState.Minimized;
            CaptureScreenshot(1);
            this.WindowState = WindowState.Normal;
            this.Activate();
            btnCapture1.IsEnabled = true;
        }

        private void BtnCapture2_Click(object sender, RoutedEventArgs e)
        {
            if (isCapturing) return;
            btnCapture2.IsEnabled = false;
            this.WindowState = WindowState.Minimized;
            CaptureScreenshot(2);
            this.WindowState = WindowState.Normal;
            this.Activate();
            btnCapture2.IsEnabled = true;
        }

        private void BtnCapture3_Click(object sender, RoutedEventArgs e)
        {
            if (isCapturing) return;
            btnCapture3.IsEnabled = false;
            this.WindowState = WindowState.Minimized;
            CaptureScreenshot(3);
            this.WindowState = WindowState.Normal;
            this.Activate();
            btnCapture3.IsEnabled = true;
        }

        private void DrmLink_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://drm.steam.run/",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void WubLink_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.sordum.org/downloads/?st-windows-update-blocker",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void CaptureScreenshot(int screenshotNumber)
        {
            // Prevent multiple simultaneous captures
            if (isCapturing)
            {
                return;
            }

            isCapturing = true;

            try
            {
                // Determine screenshot name based on number
                string screenshotName = screenshotNumber switch
                {
                    1 => "1_DRM_Authorization.png",
                    2 => "2_Steam_Client.png",
                    3 => "3_Windows_Update_Blocker.png",
                    _ => $"screenshot_{screenshotNumber}.png"
                };

                // If in testing mode, create a fake screenshot instead
                if (testingMode)
                {
                    CreateFakeScreenshot(screenshotDirectory, screenshotName, screenshotNumber);

                    string screenshotPath = Path.Combine(screenshotDirectory, screenshotName);
                    if (screenshotNumber == 1)
                    {
                        screenshot1Path = screenshotPath;
                        lblScreenshot1Status.Text = $"✓ Captured: {Path.GetFileName(screenshot1Path)} [FAKE]";
                        lblScreenshot1Status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74));
                    }
                    else if (screenshotNumber == 2)
                    {
                        screenshot2Path = screenshotPath;
                        lblScreenshot2Status.Text = $"✓ Captured: {Path.GetFileName(screenshot2Path)} [FAKE]";
                        lblScreenshot2Status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74));
                    }
                    else if (screenshotNumber == 3)
                    {
                        screenshot3Path = screenshotPath;
                        lblScreenshot3Status.Text = $"✓ Captured: {Path.GetFileName(screenshot3Path)} [FAKE]";
                        lblScreenshot3Status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74));
                    }

                    CheckCompletion();
                    return;
                }

                // Normal screenshot capture mode
                // Don't hide - SnippingWindow takes over the entire screen anyway

                // Wait a moment for user to prepare
                System.Threading.Thread.Sleep(500);

                // Capture the screen BEFORE creating the overlay window
                var (screenCapture, offsetX, offsetY) = CaptureAllScreens();

                // Open the snipping tool with the pre-captured screen and offset
                var snippingWindow = new SnippingWindow(screenshotDirectory, screenCapture, offsetX, offsetY, screenshotName);
                var result = snippingWindow.ShowDialog();

                // Snipping window closed, continue

                if (result == true && snippingWindow.CapturedImagePath != null)
                {
                    if (screenshotNumber == 1)
                    {
                        screenshot1Path = snippingWindow.CapturedImagePath;
                        lblScreenshot1Status.Text = $"✓ Captured: {Path.GetFileName(screenshot1Path)}";
                        lblScreenshot1Status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74));
                    }
                    else if (screenshotNumber == 2)
                    {
                        screenshot2Path = snippingWindow.CapturedImagePath;
                        lblScreenshot2Status.Text = $"✓ Captured: {Path.GetFileName(screenshot2Path)}";
                        lblScreenshot2Status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74));
                    }
                    else if (screenshotNumber == 3)
                    {
                        screenshot3Path = snippingWindow.CapturedImagePath;
                        lblScreenshot3Status.Text = $"✓ Captured: {Path.GetFileName(screenshot3Path)}";
                        lblScreenshot3Status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74));
                    }

                    CheckCompletion();
                }
            }
            finally
            {
                isCapturing = false;
            }
        }

        private void CreateFakeScreenshot(string directory, string filename, int screenshotNumber)
        {
            var path = Path.Combine(directory, filename);

            // Create a simple 100x100 pink bitmap
            using (var bitmap = new System.Drawing.Bitmap(100, 100))
            {
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.Clear(System.Drawing.Color.HotPink);

                    // Draw text on the image
                    using (var font = new System.Drawing.Font("Arial", 10))
                    using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                    {
                        g.DrawString($"FAKE\nTEST\n#{screenshotNumber}", font, brush, 10, 25);
                    }
                }

                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void CheckCompletion()
        {
            if (screenshot1Path != null && screenshot2Path != null && screenshot3Path != null)
            {
                btnFinish.IsEnabled = true;
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = screenshotDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to open folder: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnFinish_Click(object sender, RoutedEventArgs e)
        {
            // Set DialogResult based on whether all screenshots are captured
            if (screenshot1Path != null && screenshot2Path != null && screenshot3Path != null)
            {
                this.DialogResult = true;
            }
            else
            {
                this.DialogResult = false;
            }
        }

        private (System.Drawing.Bitmap bitmap, int offsetX, int offsetY) CaptureAllScreens()
        {
            // Get the bounds of all screens combined (multi-monitor support)
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (var screen in WinFormsScreen.AllScreens)
            {
                minX = Math.Min(minX, screen.Bounds.X);
                minY = Math.Min(minY, screen.Bounds.Y);
                maxX = Math.Max(maxX, screen.Bounds.Right);
                maxY = Math.Max(maxY, screen.Bounds.Bottom);
            }

            int width = maxX - minX;
            int height = maxY - minY;

            // Create a bitmap of all screens
            var bitmap = new System.Drawing.Bitmap(
                width,
                height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    minX,
                    minY,
                    0,
                    0,
                    new System.Drawing.Size(width, height),
                    System.Drawing.CopyPixelOperation.SourceCopy);
            }

            // Return bitmap and the offset (negative coordinates for left/top monitors)
            return (bitmap, minX, minY);
        }
    }
}
