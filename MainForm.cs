using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SteamGameVerifier
{
    public partial class MainForm : Form
    {
        private TextBox txtAppId = null!;
        private Button btnVerify = null!;
        private Button btnRestartSteam = null!;
        private TextBox txtLuaPath = null!;
        private Button btnBrowseLua = null!;
        private RichTextBox txtOutput = null!;
        private Label lblStatus = null!;
        private CheckBox chkIncludeLua = null!;
        private GameInfo? currentGameInfo;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "LuaTools - Steam Game Install Checker";
            this.Size = new System.Drawing.Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(800, 600);

            // AppID Input Section
            var lblAppId = new Label
            {
                Text = "Steam AppID:",
                Location = new System.Drawing.Point(20, 20),
                AutoSize = true
            };

            txtAppId = new TextBox
            {
                Location = new System.Drawing.Point(120, 18),
                Size = new System.Drawing.Size(150, 23),
                PlaceholderText = "e.g., 322170"
            };

            btnVerify = new Button
            {
                Text = "Verify & Generate Report",
                Location = new System.Drawing.Point(280, 15),
                Size = new System.Drawing.Size(180, 28),
                BackColor = System.Drawing.Color.FromArgb(66, 133, 244),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnVerify.FlatAppearance.BorderSize = 0;
            btnVerify.Click += BtnVerify_Click;

            // Lua File Section
            var lblLua = new Label
            {
                Text = "Lua File (Optional):",
                Location = new System.Drawing.Point(20, 60),
                AutoSize = true
            };

            chkIncludeLua = new CheckBox
            {
                Location = new System.Drawing.Point(20, 60),
                Size = new System.Drawing.Size(140, 20),
                Text = "Include Lua File:"
            };
            chkIncludeLua.CheckedChanged += ChkIncludeLua_CheckedChanged;

            txtLuaPath = new TextBox
            {
                Location = new System.Drawing.Point(160, 58),
                Size = new System.Drawing.Size(350, 23),
                Enabled = false
            };

            btnBrowseLua = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(520, 56),
                Size = new System.Drawing.Size(80, 28),
                Enabled = false
            };
            btnBrowseLua.Click += BtnBrowseLua_Click;

            // Restart Steam Button
            btnRestartSteam = new Button
            {
                Text = "Reset Activation & Restart Steam",
                Location = new System.Drawing.Point(20, 100),
                Size = new System.Drawing.Size(250, 28),
                BackColor = System.Drawing.Color.FromArgb(255, 87, 34),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnRestartSteam.FlatAppearance.BorderSize = 0;
            btnRestartSteam.Click += BtnRestartSteam_Click;

            // Output Section
            var lblOutput = new Label
            {
                Text = "Output Preview:",
                Location = new System.Drawing.Point(20, 140),
                AutoSize = true
            };

            txtOutput = new RichTextBox
            {
                Location = new System.Drawing.Point(20, 165),
                Size = new System.Drawing.Size(840, 420),
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 9),
                BackColor = System.Drawing.Color.FromArgb(30, 30, 30),
                ForeColor = System.Drawing.Color.FromArgb(220, 220, 220)
            };

            // Status Bar
            lblStatus = new Label
            {
                Text = "Ready. Enter an AppID to begin.",
                Location = new System.Drawing.Point(20, 595),
                Size = new System.Drawing.Size(840, 20),
                AutoSize = false,
                ForeColor = System.Drawing.Color.Gray
            };

            // Save Report Button
            var btnSave = new Button
            {
                Text = "Save Report to File",
                Location = new System.Drawing.Point(20, 620),
                Size = new System.Drawing.Size(150, 28),
                Enabled = false
            };
            btnSave.Click += BtnSave_Click;
            btnSave.Tag = "btnSave"; // Tag for enabling later

            // Add all controls
            this.Controls.Add(lblAppId);
            this.Controls.Add(txtAppId);
            this.Controls.Add(btnVerify);
            this.Controls.Add(chkIncludeLua);
            this.Controls.Add(txtLuaPath);
            this.Controls.Add(btnBrowseLua);
            this.Controls.Add(btnRestartSteam);
            this.Controls.Add(lblOutput);
            this.Controls.Add(txtOutput);
            this.Controls.Add(lblStatus);
            this.Controls.Add(btnSave);

            // Store save button reference
            btnVerify.Tag = btnSave;
        }

        private void ChkIncludeLua_CheckedChanged(object? sender, EventArgs e)
        {
            txtLuaPath.Enabled = chkIncludeLua.Checked;
            btnBrowseLua.Enabled = chkIncludeLua.Checked;
        }

        private void BtnBrowseLua_Click(object? sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Lua Files (*.lua)|*.lua|All Files (*.*)|*.*",
                Title = "Select Lua File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtLuaPath.Text = openFileDialog.FileName;
            }
        }

        private void BtnVerify_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAppId.Text))
            {
                MessageBox.Show("Please enter a Steam AppID.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                lblStatus.Text = "Verifying game installation...";
                lblStatus.ForeColor = System.Drawing.Color.Orange;
                Application.DoEvents();

                // Get game info
                currentGameInfo = SteamHelper.GetGameInfo(txtAppId.Text.Trim());

                // Calculate actual folder size
                lblStatus.Text = "Calculating folder size...";
                Application.DoEvents();
                long actualSize = SteamHelper.GetDirectorySize(currentGameInfo.GamePath);

                // Generate folder tree
                lblStatus.Text = "Generating folder structure...";
                Application.DoEvents();
                string folderTree = SteamHelper.GenerateFolderTree(currentGameInfo.GamePath);

                // Build output report
                var report = GenerateReport(currentGameInfo, actualSize, folderTree);

                // Display in output
                txtOutput.Text = report;

                lblStatus.Text = $"✓ Verification complete for {currentGameInfo.Name}";
                lblStatus.ForeColor = System.Drawing.Color.Green;

                // Enable save button and restart button
                if (btnVerify.Tag is Button btnSave)
                    btnSave.Enabled = true;

                btnRestartSteam.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Verification Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "✗ Verification failed.";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
        }

        private string GenerateReport(GameInfo gameInfo, long actualSize, string folderTree)
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

            // Include Lua file if checkbox is checked
            if (chkIncludeLua.Checked && !string.IsNullOrWhiteSpace(txtLuaPath.Text))
            {
                if (File.Exists(txtLuaPath.Text))
                {
                    sb.AppendLine();
                    sb.AppendLine("───────────────────────────────────────────────────────────────");
                    sb.AppendLine("LUA FILE CONTENTS (FORMATTED)");
                    sb.AppendLine("───────────────────────────────────────────────────────────────");
                    sb.AppendLine();
                    sb.AppendLine($"File: {Path.GetFileName(txtLuaPath.Text)}");
                    sb.AppendLine();
                    sb.AppendLine(SteamHelper.ReadAndFormatLuaFile(txtLuaPath.Text));
                }
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("          END OF REPORT - GENERATED BY LUATOOLS");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            return sb.ToString();
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (currentGameInfo == null)
            {
                MessageBox.Show("No report to save. Please verify a game first.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var saveFileDialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"GameVerification_{currentGameInfo.AppId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                Title = "Save Verification Report"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, txtOutput.Text);
                    lblStatus.Text = $"✓ Report saved to: {saveFileDialog.FileName}";
                    lblStatus.ForeColor = System.Drawing.Color.Green;

                    MessageBox.Show($"Report saved successfully!\n\n{saveFileDialog.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save report: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnRestartSteam_Click(object? sender, EventArgs e)
        {
            if (currentGameInfo == null)
            {
                MessageBox.Show("Please verify a game first before resetting activation.", "No Game Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"WARNING: This will reset the activation status for:\n\n" +
                $"{currentGameInfo.Name} (AppID: {currentGameInfo.AppId})\n\n" +
                "This will:\n" +
                "- Clear the Steam ID for this game using steam://run/tool/clearsteamid\n" +
                "- Close all running Steam games\n" +
                "- Restart Steam\n\n" +
                "You will need to provide DRM screenshots again after restart.\n\n" +
                "Do you want to continue?",
                "Confirm Reset Activation & Restart Steam",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    lblStatus.Text = "Resetting activation and restarting Steam...";
                    lblStatus.ForeColor = System.Drawing.Color.Orange;
                    Application.DoEvents();

                    SteamHelper.ResetActivationAndRestartSteam(currentGameInfo.AppId);

                    lblStatus.Text = "✓ Activation reset and Steam restarted successfully.";
                    lblStatus.ForeColor = System.Drawing.Color.Green;

                    MessageBox.Show(
                        "Steam activation has been reset and Steam has been restarted!\n\n" +
                        "You can now provide DRM screenshots for support.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to reset activation and restart Steam: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "✗ Failed to reset activation and restart Steam.";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                }
            }
        }
    }
}
