# Usage Guide - LuaTools Game Install Checker

## Quick Start

### Step 1: Build the Application
Run `build.bat` to compile the application. This will automatically detect if you have .NET SDK or Visual Studio installed.

### Step 2: Run the Application
After building, run:
```
bin\Release\net8.0-windows\SteamGameVerifier.exe
```

### Step 3: Enter AppID
In the application window:
1. Enter a Steam AppID (e.g., `2009350` for "Out of Ore")
2. Click **"Verify & Generate Report"**

### Step 4: (Optional) Include Lua File
If you want to include a Lua file in the report:
1. Check the **"Include Lua File:"** checkbox
2. Click **"Browse..."** and select your .lua file
3. The Lua file contents will be formatted and included in the report

### Step 5: Save the Report
1. Review the report in the preview window
2. Click **"Save Report to File"**
3. Choose a location and filename
4. Send this file to support!

## Example Test Cases

You can test with these AppIDs from your Steam library:

| AppID   | Game Name        | Library Location |
|---------|------------------|------------------|
| 2009350 | Out of Ore       | C:\Program Files (x86)\Steam |
| 228980  | Steamworks Common| C:\Program Files (x86)\Steam |
| 220240  | (Check library)  | D:\SteamLibrary |
| 220     | Half-Life 2      | E:\SteamLibrary |

## Steam Restart Feature

After making Lua changes to games, you can use the **"Reset Activation & Restart Steam"** button to:
- Kill all Steam processes
- Wait for clean shutdown
- Restart Steam automatically

**Warning:** This will:
- Close all running Steam games
- Reset your current activation status
- Require you to provide DRM screenshots again after restart

## Report Contents Explanation

### Installation Details Section
- **Install Directory Path**: Full path to the game folder
- **Folder Size (Actual)**: Calculated by scanning all files in the folder
- **Folder Size (Steam)**: Size reported in the Steam manifest file
- **Build ID**: Game build/version number from Steam

### Folder Structure Section
Shows all files and folders at the root level of the game directory:
- `[FolderName]` = Directory/Folder
- `FileName` = File

### Lua File Section (if included)
Shows the contents of the selected Lua file with formatting:
- Lines starting with `addappid` are left-aligned
- All other non-empty lines are indented with 4 spaces

## Troubleshooting

### "Steam installation not found"
- Make sure Steam is installed
- Run the app as Administrator if needed

### "Game with AppID X not found"
- Double-check the AppID is correct
- Verify the game is actually installed in Steam
- Try right-clicking the game in Steam → Properties → check AppID in the URL

### Build errors
- Install .NET 8.0 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
- Or install Visual Studio 2022 with ".NET desktop development" workload

### Application won't start
- Install .NET 8.0 Desktop Runtime: https://dotnet.microsoft.com/download/dotnet/8.0
- Make sure you're running on Windows (this is a Windows-only app)

## For Melly's Support Workflow

### Before this tool:
Users needed to provide 4+ screenshots:
1. Lua file in notepad
2. Game folder properties window
3. Game folder file list
4. LuaTools updates disabled window

### With this tool:
Users:
1. Enter AppID
2. Select Lua file (if applicable)
3. Click "Verify & Generate Report"
4. Save and send ONE text file

This text file contains all the information from those 4 screenshots in a structured, copy-pasteable format!

## Advanced Usage

### Batch Processing
If you need to verify multiple games, you can:
1. Verify first game → Save report
2. Enter next AppID → Verify → Save report
3. Repeat as needed

The reports are timestamped and include the AppID in the suggested filename.

### Custom Lua File Locations
The Lua file can be located anywhere on your system. Common locations:
- LuaTools install directory
- Game-specific configuration folders
- Custom script directories

Just browse to the file when the checkbox is enabled.

## Integration with LuaTools Support

Support staff can request users to:
1. Download this tool
2. Run it with the problematic game's AppID
3. Include the Lua configuration file
4. Send the generated .txt report

The report contains all diagnostic information needed to:
- Verify game is properly installed
- Check Build ID matches expected version
- Review Lua configuration
- See folder structure for missing files
- Confirm folder sizes match expectations
