# Release Process

## How to Create a New Release

This project uses GitHub Actions to automatically build and release versions when tags are pushed.

### Version Numbering

Starting from **v1.2**, all major versions follow the pattern `v1.x`:
- v1.2 - Initial release with workshop support
- v1.3 - Next major version
- v1.4 - Next major version
- etc.

### Creating a Release

1. **Update the version in the project file:**
   ```bash
   # Edit SteamGameVerifierWPF.csproj
   # Change <Version>1.2.0</Version> to your new version
   ```

2. **Commit your changes:**
   ```bash
   git add -A
   git commit -m "Bump version to 1.x.0 and add feature descriptions"
   ```

3. **Create and push the tag:**
   ```bash
   git tag -a v1.x -m "Release v1.x - Description of changes"
   git push origin master --tags
   ```

4. **GitHub Actions will automatically:**
   - Build the self-contained executable
   - Create a compressed .zip file
   - Create a GitHub release
   - Upload the executable as a release asset

### What Gets Released

Each release includes:
- **LuaToolsGameChecker-v1.x.zip** - Contains the self-contained executable
- File size: ~68 MB (includes .NET 8 runtime)
- Platform: Windows 10/11 x64

### Release Notes Template

When creating a new version, update the GitHub Actions workflow release body if needed to describe new features.

Current features included:
- Steam game verification tool
- DLC update management (Enable/Disable)
- Workshop decryption key support
- Activation reset functionality
- DRM screenshot wizard
- Automated report generation

### Testing Before Release

Before creating a tag, always test locally:
```bash
dotnet publish SteamGameVerifierWPF.csproj -c Release -r win-x64 --self-contained true
```

Verify the executable works at:
```
bin/Release/net8.0-windows/win-x64/publish/LuaToolsGameChecker.exe
```
