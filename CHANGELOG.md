# Changelog

All notable changes to LuaTools Game Install Checker will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.7.0] - 2025-11-05

### Added
- **SizeOnDisk Validation** - Pre-flight check for game installation status
  - Validates game is actually installed before allowing operations
  - Checks ACF file `SizeOnDisk` field (0 = not installed)
  - Shows clear error message when game has no files on disk
  - Prevents user confusion from operating on uninstalled games
  - Status indicator shows "Game not installed (SizeOnDisk = 0)"

- **Clear SteamID Button** - Standalone activation reset tool
  - New button in title bar next to "Update Whitelist"
  - Allows clearing Steam ID without loading a game first
  - Prompts for AppID input with validation
  - Performs same activation reset as "Reset Activation" button
  - Fully themed to match application design
  - Workflow:
    - Kills Steam processes
    - Restarts Steam
    - Launches game once
    - Clears Steam ID using `steam://run/tool/clearsteamid/{appId}`

### Changed
- Game loading now validates installation status before proceeding
- Clear SteamID functionality available independently of game load workflow
- Input dialog styling matches Steam-inspired theme perfectly
  - CardBackgroundBrush with AccentBrush border
  - Rounded corners (6px) on all elements
  - Proper text and button centering
  - Drop shadow effects

### Fixed
- Users can no longer attempt operations on games that aren't installed
- Prevented potential errors from missing game files

## [1.6.1] - 2025-10-31

### Added
- **Morrenus Signature Validation** - Enhanced file validation on every load
  - Checks for specific signature comment: `-- {appId}'s Lua and Manifest Created by Morrenus`
  - Validation occurs every time "Load Game" is clicked
  - Prevents bypass attempts by validating actual file contents instead of flags
  - User-friendly error messages that guide without exposing technical details

### Changed
- **Breaking**: Removed blocking verification flag system
- **Simplified Verification Flow** - `steam://validate` runs automatically without user prompts
  - No more "Did you verify?" confirmation dialogs
  - Automatic background verification after Morrenus download
  - Removed verification pending flags that could block users
- Replaced all `MessageBox` calls with `CustomMessageBox` for UI consistency
- Error messages now inform users about automatic download option

### Removed
- Verification flag blocking system (`.verification_pending_{appId}` files)
- Manual verification confirmation prompts
- Complex verification tracking logic

### Fixed
- Files without Morrenus signature are now properly detected and rejected on every load
- Users cannot bypass validation by replacing Lua files with old versions
- Cleaner verification workflow without unnecessary user interaction

## [1.6.0] - 2025-10-31

### Added
- **Verification Enforcement System** - Mandatory Steam validation after Morrenus file installation
  - Automatically triggers `steam://validate/<AppID>` dialog after Steam restart
  - User confirms completion with Yes/No dialog
  - Blocks application usage if verification is not completed
  - Improved verification workflow reliability

- **Startup Warning Dialog** - Report integrity policy shown when app opens
  - Warns against faking/modifying reports or bypassing verification
  - Permanent Denuvo activation loss consequence for fraudulent reports
  - Clear acknowledgment required before using application

- **Enhanced User Experience**
  - Simplified verification instructions
  - Clear blocking messages when verification is required
  - "Be honest" reminder in confirmation dialogs
  - Better status feedback during verification process

### Changed
- **Breaking**: Morrenus file installation now requires mandatory Steam validation
- **Breaking**: Application blocks game loading if verification is pending
- Trust-based verification confirmation (honor system)
- Extended Steam initialization wait time from 5 to 8 seconds after restart
- Improved message box formatting and clarity

### Removed
- Manual "verify through Steam" prompts without enforcement
- Complex ACF timestamp monitoring (doesn't work reliably)

### Fixed
- Improved reliability of Steam validation workflow
- Better enforcement of verification requirements
- Cleaner namespace usage to avoid ambiguity

## [1.5.1] - 2025-10-27

### Added
- Automatic Steam restart after downloading Morrenus files from API
- Game file verification prompt with step-by-step instructions after Morrenus installation
- User confirmation dialog to ensure verification is completed before continuing game load
- RestartSteam() helper method for reliable Steam restart process

### Changed
- Morrenus download workflow now includes mandatory Steam restart
- User must manually verify game files through Steam after Morrenus installation
- Status messages updated to reflect new verification requirement

### Fixed
- Improved reliability of Morrenus Lua files being loaded by Steam
- Ensures game files are verified after installing new Lua/manifest files from API

## [1.5.0] - 2025-10-27

### Added
- **Denuvo Whitelist System**
  - Whitelist validation before loading games - only Denuvo titles on the official list are supported
  - Auto-update on startup if whitelist is older than 7 days
  - Manual "Update Whitelist" button in top-right corner for force refresh
  - Dual-source download with automatic fallback:
    - Primary: `https://raw.githubusercontent.com/madoiscool/lt_api_links/refs/heads/main/denuvoappids`
    - Fallback: `https://luatools.vercel.app/denuvoappids` (on GitHub 429 rate limit)
  - Whitelist stored locally at `Documents\LuaTools Reports\denuvo_whitelist.txt`
  - Shows count of supported games after update

- **Morrenus Auto-Download & Extraction**
  - Automatic detection when Morrenus files are missing for a game
  - One-click download prompt from `https://mellyiscoolaf.pythonanywhere.com/m/<appid>`
  - Auto-extraction to correct directories:
    - `.lua` files → `Steam\config\stplug-in\`
    - `.manifest` files → `Steam\depotcache\`
  - Progress feedback during download and extraction
  - Smart detection: searches for .lua files containing "morrenus" and the AppID

- **App Auto-Update System**
  - Automatic update check on app startup
  - Downloads latest version from GitHub releases
  - Progress indicator during download
  - Seamless update: downloads, replaces exe, and restarts automatically
  - User consent required before updating
  - Version comparison using file size detection

- **New Helper Classes**
  - `WhitelistManager.cs` - Manages Denuvo whitelist operations
  - `UpdateManager.cs` - Handles app self-update functionality
  - `MorrenusDownloader.cs` - Downloads and extracts Morrenus files

### Changed
- **Breaking**: AppID load now requires game to be on Denuvo whitelist
- **Breaking**: Morrenus files are now required - load fails if user declines download
- Updated GitHub Actions workflow to produce single executable: `LuaToolsGameInstallChecker.exe`
- Release URL now stable for auto-update feature: `/releases/latest/download/LuaToolsGameInstallChecker.exe`
- BtnLoadGame_Click() converted to async method for download operations
- Enhanced error messages with clearer instructions

### Fixed
- Better error handling for network failures
- Graceful handling of GitHub rate limits (429 errors)
- Improved timeout handling for various operations (10-120 seconds)

### Technical Details
- HTTP client timeouts:
  - Whitelist: 10 seconds
  - Update check: 10 seconds (HEAD request)
  - Update download: 60 seconds
  - Morrenus download: 120 seconds
- All downloads use async/await pattern
- UI updates from background threads via Dispatcher.Invoke()
- In-memory caching of whitelist for performance

## [1.4.0] - 2025-10-21

### Fixed
- DPI scaling bug causing "half screenshot" on 125%+ scaling
- Multi-monitor coordinate offset (changed subtraction to addition)
- Screenshot window now spans all monitors instead of just primary
- Proper conversion of WPF DIPs to physical pixels in coordinate calculation
- Support for all DPI scaling levels: 100%, 125%, 150%, 200%, 250%

## [1.3.0] - 2025-10-19

### Fixed
- Fixed depot line commenting to preserve main app workshop keys
- DisableDlcUpdates now only comments DLC depot lines
- Main app depot lines (matching mainAppId) now stay active
- Preserves workshop decryption keys for the main game
- Only comments depot lines where first parameter != mainAppId

### Changed
- Improved Lua file error messages

## [1.2.0] - 2025-10-19

### Added
- Initial public release
- Workshop decryption key support
- Improved Lua file detection
- GitHub Actions release workflow
- Automated build and release process

### Changed
- Updated workflow to release compressed exe directly instead of zip archive
- Generic paths in README instead of hardcoded user paths

## [1.1.0] - 2025-10-14

### Added
- Disable/Enable DLC updates functionality
- Steam restart and activation reset
- DRM screenshot wizard
- Automated report generation
- Folder structure analysis
- Game verification system

[1.6.1]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.6.1
[1.6.0]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.6.0
[1.5.1]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.5.1
[1.5.0]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.5.0
[1.4.0]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.4.0
[1.3.0]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.3.0
[1.2.0]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.2.0
