using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinFormsScreen = System.Windows.Forms.Screen;

namespace LuaToolsGameChecker
{
    public partial class SnippingWindow : Window
    {
        private System.Windows.Point startPoint;
        private bool isSelecting = false;
        private string saveDirectory;
        private int screenOffsetX;
        private int screenOffsetY;
        private string screenshotName;

        public string? CapturedImagePath { get; private set; }

        public SnippingWindow(string saveDirectory, System.Drawing.Bitmap screenCapture, int offsetX, int offsetY, string screenshotName)
        {
            InitializeComponent();
            this.saveDirectory = saveDirectory;
            this.screenOffsetX = offsetX;
            this.screenOffsetY = offsetY;
            this.screenshotName = screenshotName;

            // Store the pre-captured bitmap
            this.Tag = screenCapture;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(this.canvas);
            isSelecting = true;
            selectionRectangle.Visibility = Visibility.Visible;
            System.Windows.Controls.Canvas.SetLeft(selectionRectangle, startPoint.X);
            System.Windows.Controls.Canvas.SetTop(selectionRectangle, startPoint.Y);
            selectionRectangle.Width = 0;
            selectionRectangle.Height = 0;
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isSelecting)
            {
                var currentPoint = e.GetPosition(this.canvas);
                var x = Math.Min(startPoint.X, currentPoint.X);
                var y = Math.Min(startPoint.Y, currentPoint.Y);
                var width = Math.Abs(currentPoint.X - startPoint.X);
                var height = Math.Abs(currentPoint.Y - startPoint.Y);

                System.Windows.Controls.Canvas.SetLeft(selectionRectangle, x);
                System.Windows.Controls.Canvas.SetTop(selectionRectangle, y);
                selectionRectangle.Width = width;
                selectionRectangle.Height = height;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (isSelecting)
            {
                isSelecting = false;
                CaptureSelection();
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                CapturedImagePath = null;
                this.DialogResult = false;
                this.Close();
            }
        }

        private void CaptureSelection()
        {
            var x = System.Windows.Controls.Canvas.GetLeft(selectionRectangle);
            var y = System.Windows.Controls.Canvas.GetTop(selectionRectangle);
            var width = selectionRectangle.Width;
            var height = selectionRectangle.Height;

            if (width < 10 || height < 10)
            {
                // Selection too small, cancel
                CapturedImagePath = null;
                this.DialogResult = false;
                this.Close();
                return;
            }

            try
            {
                var screenBitmap = this.Tag as System.Drawing.Bitmap;
                if (screenBitmap != null)
                {
                    // Convert window coordinates to screen bitmap coordinates
                    // Account for the screen offset (for multi-monitor setups)
                    int screenX = (int)x - screenOffsetX;
                    int screenY = (int)y - screenOffsetY;

                    // Crop the bitmap to the selected area
                    var cropRect = new System.Drawing.Rectangle(
                        screenX,
                        screenY,
                        (int)width,
                        (int)height);

                    var croppedBitmap = screenBitmap.Clone(cropRect, screenBitmap.PixelFormat);

                    // Save the cropped image with descriptive name
                    var filepath = Path.Combine(saveDirectory, screenshotName);

                    croppedBitmap.Save(filepath, System.Drawing.Imaging.ImageFormat.Png);

                    CapturedImagePath = filepath;
                    this.DialogResult = true;
                }
                else
                {
                    CapturedImagePath = null;
                    this.DialogResult = false;
                }

                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to capture screenshot: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                CapturedImagePath = null;
                this.DialogResult = false;
                this.Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Dispose the bitmap
            var bitmap = this.Tag as System.Drawing.Bitmap;
            bitmap?.Dispose();
            base.OnClosed(e);
        }
    }
}
