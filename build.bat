@echo off
echo ========================================
echo LuaTools Game Install Checker - Builder
echo ========================================
echo.

REM Check if dotnet is available
where dotnet >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [*] .NET SDK found, building single-file compressed executable...
    dotnet publish SteamGameVerifierWPF.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo [SUCCESS] Build completed!
        echo Executable location: bin\Release\net8.0-windows\win-x64\publish\LuaToolsGameChecker.exe
        pause
        exit /b 0
    ) else (
        echo.
        echo [ERROR] Build failed!
        pause
        exit /b 1
    )
)

REM Try to find MSBuild
echo [*] .NET SDK not found, searching for MSBuild...

set MSBUILD_PATH=""

REM Check common Visual Studio 2022 locations
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)

if %MSBUILD_PATH%=="" (
    echo.
    echo [ERROR] Neither .NET SDK nor MSBuild found!
    echo.
    echo Please install one of the following:
    echo 1. .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo 2. Visual Studio 2022 with C# support
    echo.
    pause
    exit /b 1
)

echo [*] MSBuild found, building single-file compressed executable...
%MSBUILD_PATH% SteamGameVerifierWPF.csproj /p:Configuration=Release /p:RuntimeIdentifier=win-x64 /p:SelfContained=true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /t:Restore,Publish

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUCCESS] Build completed!
    echo Executable location: bin\Release\net8.0-windows\win-x64\publish\LuaToolsGameChecker.exe
) else (
    echo.
    echo [ERROR] Build failed!
)

echo.
pause
