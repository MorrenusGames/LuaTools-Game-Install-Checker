# LuaTools - Steam Game Install Checker

A C# WinForms application that verifies Steam game installations and generates diagnostic reports for support purposes.

## Features

- **Automatic Steam Detection**: Finds Steam installation via Windows Registry
- **Multi-Library Support**: Scans all Steam library folders
- **Game Verification**: Validates game installation by AppID
- **Detailed Reports**: Generates comprehensive text reports including:
  - Game folder directory path
  - Actual folder size (calculated)
  - Steam manifest folder size
  - Build ID from Steam manifest
  - Folder structure tree (root level files/folders)
  - Optional: Formatted Lua file contents
- **Steam Restart**: Built-in functionality to restart Steam after Lua changes
- **Export Reports**: Save verification reports as text files

## Requirements

- Windows OS
- .NET 8.0 Runtime or SDK
- Steam installed

## Building the Application

### Using Visual Studio 2022
1. Open `SteamGameVerifier.sln` in Visual Studio (or the folder in VS Code)
2. Build > Build Solution (or press F6)
3. Run the application (F5)

### Using .NET CLI
```bash
cd "C:\Morrenus Stuff\LuaTools Game Install Checker"
dotnet build
dotnet run
```

### Using MSBuild (if you have Visual Studio installed)
```bash
cd "C:\Morrenus Stuff\LuaTools Game Install Checker"
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" SteamGameVerifier.csproj
```

## Usage

1. **Enter AppID**: Type the Steam AppID of the game you want to verify
2. **(Optional) Include Lua File**: Check the box and browse to select a .lua file to include in the report
3. **Click "Verify & Generate Report"**: The app will:
   - Find the game in your Steam libraries
   - Calculate the actual folder size
   - Generate a folder structure tree
   - Display the report in the preview window
4. **Save Report**: Click "Save Report to File" to export as a .txt file
5. **(Optional) Restart Steam**: Click "Reset Activation & Restart Steam" to restart Steam (warns about killing current activation)

## Report Output Format

The generated report includes:

```
═══════════════════════════════════════════════════════════════
   LUATOOLS - STEAM GAME INSTALLATION VERIFICATION REPORT
═══════════════════════════════════════════════════════════════

Generated: 2025-10-14 10:30:00
AppID: 322170
Game Name: Geometry Dash

───────────────────────────────────────────────────────────────
INSTALLATION DETAILS
───────────────────────────────────────────────────────────────

Install Directory Path:
  K:\steam client\steamapps\common\Geometry Dash

Folder Size (Actual): 313.66 MB (328,749,547 bytes)
Folder Size (Steam):  313.66 MB (328,749,547 bytes)

Build ID: 16373064

───────────────────────────────────────────────────────────────
FOLDER STRUCTURE (ROOT LEVEL)
───────────────────────────────────────────────────────────────

Geometry Dash\
  [Resources]
  [steam_api.dll]
  GeometryDash.exe
  ...

───────────────────────────────────────────────────────────────
LUA FILE CONTENTS (FORMATTED)
───────────────────────────────────────────────────────────────

File: game_config.lua

addappid(322170)
    config_setting_1 = true
    config_setting_2 = "value"
    ...
```

## Files

- `SteamGameVerifier.csproj` - Project file
- `Program.cs` - Application entry point
- `MainForm.cs` - Main GUI form with all UI logic
- `SteamHelper.cs` - Core Steam-related functionality:
  - Registry reading
  - VDF/ACF parsing
  - Folder size calculation
  - Tree generation
  - Steam process management

## How It Works

1. **Registry Detection**: Reads Steam install path from `HKLM\SOFTWARE\WOW6432Node\Valve\Steam`
2. **Library Scanning**: Parses `libraryfolders.vdf` to find all Steam library locations
3. **Game Location**: Searches for `appmanifest_<appid>.acf` in each library's steamapps folder
4. **Manifest Parsing**: Extracts `installdir`, `buildid`, and `SizeOnDisk` from the ACF file
5. **Folder Analysis**: Calculates actual folder size and generates structure tree
6. **Report Generation**: Formats all data into a readable text report

## Troubleshooting

**"Steam installation not found"**
- Ensure Steam is installed
- Check that Steam registry keys exist

**"Game with AppID X not found"**
- Verify the AppID is correct
- Ensure the game is installed in Steam
- Try verifying game files in Steam first

**".NET SDK not found"**
- Install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
- Or use Visual Studio 2022 to build

## Use Case

This tool is designed to automate the collection of game installation information for support tickets, replacing the need for users to manually provide:
- Screenshots of Lua files
- Windows properties tab screenshots
- Game folder screenshots
- LuaTools update status screenshots

Users simply enter the AppID, optionally select their Lua file, generate the report, and send the output text file to support.

## Author

Created for LuaTools support workflow automation.
