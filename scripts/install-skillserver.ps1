#Requires -Version 5.1
<#
.SYNOPSIS
    Install the skillserver CLI on Windows.
.DESCRIPTION
    Downloads and installs the skillserver CLI binary from GitHub Releases.
    No administrator permissions required - installs to your home directory.
.PARAMETER Version
    Version to install. Defaults to "latest".
.PARAMETER InstallDir
    Installation directory. Defaults to $env:LOCALAPPDATA\Programs\skillserver.
.EXAMPLE
    iwr -useb https://raw.githubusercontent.com/netclaw-dev/skill-server/dev/scripts/install-skillserver.ps1 | iex
.EXAMPLE
    .\install-skillserver.ps1 -Version 0.2.1
#>
param(
    [string]$Version = "latest",
    [string]$InstallDir = ""
)

$ErrorActionPreference = "Stop"
$Repo = "netclaw-dev/skill-server"
$BinaryName = "skillserver"
$Rid = "win-x64"

if (-not $InstallDir) {
    $InstallDir = Join-Path $env:LOCALAPPDATA "Programs" $BinaryName
}

function Resolve-LatestVersion {
    if ($script:Version -eq "latest") {
        try {
            $response = Invoke-WebRequest -Uri "https://github.com/$Repo/releases/latest" `
                -MaximumRedirection 0 -ErrorAction SilentlyContinue -UseBasicParsing
            if ($response.StatusCode -eq 302) {
                $redirectUrl = $response.Headers.Location
            } else {
                $redirectUrl = $response.BaseResponse.ResponseUri.AbsoluteUri
            }
        } catch {
            $redirectUrl = $_.Exception.Response.Headers.Location.AbsoluteUri
        }
        $script:Version = ($redirectUrl -split '/')[-1]
        if (-not $script:Version) {
            throw "Could not determine latest version"
        }
    }
    $script:Version = $script:Version.TrimStart('v')
}

function Main {
    Write-Host "Installing $BinaryName..."

    Resolve-LatestVersion

    $archiveName = "$BinaryName-$Version-$Rid.zip"
    $downloadUrl = "https://github.com/$Repo/releases/download/$Version/$archiveName"
    $checksumUrl = "$downloadUrl.sha256"

    Write-Host "  Platform:  $Rid"
    Write-Host "  Version:   $Version"
    Write-Host "  Directory: $InstallDir"
    Write-Host ""

    $tmpDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
    New-Item -ItemType Directory -Path $tmpDir -Force | Out-Null

    try {
        Write-Host "  Downloading $archiveName..."
        try {
            Invoke-WebRequest -Uri $downloadUrl -OutFile (Join-Path $tmpDir $archiveName) -UseBasicParsing
        } catch {
            Write-Error "Failed to download $archiveName from $downloadUrl"
            return
        }

        Write-Host "  Verifying checksum..."
        try {
            Invoke-WebRequest -Uri $checksumUrl -OutFile (Join-Path $tmpDir "$archiveName.sha256") -UseBasicParsing
            $expectedLine = Get-Content (Join-Path $tmpDir "$archiveName.sha256") -Raw
            $expected = ($expectedLine -split '\s+')[0].ToLower()
            $actual = (Get-FileHash -Algorithm SHA256 (Join-Path $tmpDir $archiveName)).Hash.ToLower()
            if ($expected -ne $actual) {
                throw "Checksum mismatch: expected $expected, got $actual"
            }
        } catch [System.Net.WebException] {
            Write-Warning "Checksum file not available, skipping verification"
        }

        Write-Host "  Extracting..."
        if (-not (Test-Path $InstallDir)) {
            New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
        }
        Expand-Archive -Path (Join-Path $tmpDir $archiveName) -DestinationPath $InstallDir -Force

        $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
        if ($userPath -notlike "*$InstallDir*") {
            [Environment]::SetEnvironmentVariable("PATH", "$userPath;$InstallDir", "User")
            $env:PATH = "$env:PATH;$InstallDir"
            Write-Host "  Added $InstallDir to user PATH"
        }

        Write-Host ""
        Write-Host "  Installed $BinaryName $Version to $InstallDir\$BinaryName.exe"
        Write-Host ""
        Write-Host "  Run '$BinaryName --version' to verify."
        Write-Host "  You may need to restart your terminal for PATH changes to take effect."
    } finally {
        Remove-Item -Path $tmpDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Main
