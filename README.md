# LuaTools - Steam Game Install Checker

A WPF application that verifies Steam game installations, manages DLC updates, and generates diagnostic reports for support purposes.

## Features

- **Automatic Steam Detection**: Finds Steam installation via Windows Registry
- **Multi-Library Support**: Scans all Steam library folders
- **Game Verification**: Validates game installation by AppID
- **DLC Update Management**: Enable/Disable depot updates for games
- **Workshop Decryption Key Support**: Preserves workshop content access
- **Activation Reset**: Restart Steam and clear game activation
- **Screenshot Wizard**: Capture DRM status screenshots
- **Detailed Reports**: Generates comprehensive text reports including:
  - Game folder directory path
  - Actual folder size (calculated)
  - Steam manifest folder size
  - Build ID from Steam manifest
  - Folder structure tree (root level files/folders)
  - Formatted Lua file contents
- **Export Reports**: Save verification reports and screenshots

## Requirements

- Windows 10/11 x64
- .NET 8.0 Runtime (or SDK for building)
- Steam installed

## Installation

### Pre-built Release
1. Download `LuaToolsGameChecker-vX.X.exe` from [Releases](../../releases)
2. Run the executable directly (no installation needed)
3. Self-contained - includes .NET 8 runtime

### Building from Source

#### Using Visual Studio 2022
1. Open the project folder in Visual Studio
2. Build > Build Solution (or press F6)
3. Run the application (F5)

#### Using .NET CLI
```bash
cd <project-directory>
dotnet build SteamGameVerifierWPF.csproj
dotnet run --project SteamGameVerifierWPF.csproj
```

#### Build Self-Contained Executable
```bash
cd <project-directory>
dotnet publish SteamGameVerifierWPF.csproj -c Release -r win-x64 --self-contained true
```

Output: `bin/Release/net8.0-windows/win-x64/publish/LuaToolsGameChecker.exe`

## Usage

### Step 1: Load Game
1. Enter the Steam AppID of the game
2. Click "Load Game Information"
3. The app will detect the game and Lua file automatically

### Step 2: Manage Updates (Optional)
- **Disable Updates**: Comments out depot download lines, prevents file downloads
- **Enable Updates**: Uncomments depot lines, allows Steam to update files

### Step 3: Reset Activation
- Restarts Steam, launches the game once, clears Steam ID

### Step 4: Screenshot Wizard
- Capture DRM status screenshots for support tickets
- Automatically saves to report folder

### Step 5: Generate Report
- Creates comprehensive verification report
- Includes all screenshots and game information
- Opens folder with all files for support ticket submission

## Report Output Format

```
═══════════════════════════════════════════════════════════════
   LUATOOLS - STEAM GAME INSTALLATION VERIFICATION REPORT
═══════════════════════════════════════════════════════════════

Generated: 2025-10-14 10:30:00
AppID: 322170
Game Name: Example Game

───────────────────────────────────────────────────────────────
INSTALLATION DETAILS
───────────────────────────────────────────────────────────────

Install Directory Path:
  <Steam-Library>\steamapps\common\Example Game

Folder Size (Actual): 313.66 MB (328,749,547 bytes)
Folder Size (Steam):  313.66 MB (328,749,547 bytes)

Build ID: 16373064

───────────────────────────────────────────────────────────────
FOLDER STRUCTURE (ROOT LEVEL)
───────────────────────────────────────────────────────────────

Example Game\
  [Data]
  [Resources]
  steam_api.dll
  Game.exe

───────────────────────────────────────────────────────────────
LUA FILE CONTENTS (FORMATTED)
───────────────────────────────────────────────────────────────

File: game_config.lua
Path: <Steam-Install>\config\stplug-in\game_config.lua

addappid(322170)
addappid(322171, 1, "decryption_key_hash")
    setManifestid(322170, 1234567890)
```

## Project Structure

- `SteamGameVerifierWPF.csproj` - WPF project file
- `MainWindow.xaml/.cs` - Main application window
- `ScreenshotWizard.xaml/.cs` - Screenshot capture wizard
- `SnippingWindow.xaml/.cs` - Screenshot selection tool
- `WPF_SteamHelper.cs` - Core Steam functionality:
  - Registry reading
  - VDF/ACF parsing
  - Lua file detection and modification
  - Folder size calculation
  - Steam process management

## How It Works

1. **Registry Detection**: Reads Steam install path from Windows Registry
2. **Library Scanning**: Parses `libraryfolders.vdf` to find all Steam library locations
3. **Game Location**: Searches for `appmanifest_<appid>.acf` in each library
4. **Lua Detection**: Scans `<Steam>\config\stplug-in\*.lua` for matching AppID
5. **Manifest Parsing**: Extracts installation details from ACF file
6. **Folder Analysis**: Calculates actual folder size and structure
7. **DLC Management**: Modifies Lua files to enable/disable depot updates
8. **Report Generation**: Formats all data into readable text report

## Testing Mode

Enable testing mode with:
- Command line: `LuaToolsGameChecker.exe -testing`
- Keyboard shortcut: `Ctrl+Shift+T` while running

Testing mode unlocks all steps for debugging without requiring sequential completion.

## Troubleshooting

**"Steam installation not found"**
- Ensure Steam is installed properly
- Check Windows Registry for Steam keys

**"LuaTools directory not found"**
- The `<Steam>\config\stplug-in` folder doesn't exist
- Install the LuaTools plugin

**"No Lua files found"**
- The stplug-in folder exists but contains no .lua files
- Add a Lua configuration file from Sage Bot/Luie/plugin

**"AppID not found in any Lua file"**
- Lua files exist but none contain the specified AppID
- Verify you have the correct Lua file for this game

**"'morrenus' not found in Lua file"**
- Invalid LuaTools configuration file
- Refetch from Sage Bot, Luie, or the plugin

## Use Case

This tool automates the collection of game installation information for support tickets, replacing manual screenshot capture. Users can:
- Verify game installation status
- Manage DLC update behavior
- Capture DRM status screenshots
- Generate comprehensive diagnostic reports

All output files are saved to `Documents\LuaTools Reports\<AppID>_<GameName>_<Timestamp>\`

## Release Process

See [RELEASE.md](RELEASE.md) for instructions on creating new releases.

## Author

Created for LuaTools support workflow automation.
