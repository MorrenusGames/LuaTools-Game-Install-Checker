# Get version from git tags
try {
    $gitTag = git describe --tags --abbrev=0 2>$null
    if ($gitTag -match '^v?(\d+\.\d+\.\d+)') {
        $version = $matches[1]
        Write-Output $version
    } else {
        Write-Output "1.7.0"
    }
} catch {
    Write-Output "1.7.0"
}
