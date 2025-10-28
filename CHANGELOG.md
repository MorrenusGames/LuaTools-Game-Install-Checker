# Changelog

All notable changes to LuaTools Game Install Checker will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.5.1]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.5.1
[1.5.0]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.5.0
[1.4.0]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.4.0
[1.3.0]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.3.0
[1.2.0]: https://github.com/MorrenusGames/LuaTools-Game-Install-Checker/releases/tag/v1.2.0
