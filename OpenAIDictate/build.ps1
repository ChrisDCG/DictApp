# OpenAIDictate Build Script
# Builds a self-contained, single-file executable for Windows x64

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter(Mandatory=$false)]
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "OpenAIDictate Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get project directory
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ProjectDir "OpenAIDictate.csproj"
$PublishDir = Join-Path $ProjectDir "bin\$Configuration\net8.0-windows\win-x64\publish"

# Check if project file exists
if (-not (Test-Path $ProjectFile)) {
    Write-Host "ERROR: Project file not found: $ProjectFile" -ForegroundColor Red
    exit 1
}

Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Project: $ProjectFile" -ForegroundColor Yellow
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path (Join-Path $ProjectDir "bin")) {
        Remove-Item -Path (Join-Path $ProjectDir "bin") -Recurse -Force
    }
    if (Test-Path (Join-Path $ProjectDir "obj")) {
        Remove-Item -Path (Join-Path $ProjectDir "obj") -Recurse -Force
    }
    Write-Host "Clean completed." -ForegroundColor Green
    Write-Host ""
}

# Restore dependencies
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $ProjectFile
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Restore failed" -ForegroundColor Red
    exit 1
}
Write-Host "Restore completed." -ForegroundColor Green
Write-Host ""

# Build
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build $ProjectFile -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "Build completed." -ForegroundColor Green
Write-Host ""

# Publish as single-file executable
Write-Host "Publishing single-file executable..." -ForegroundColor Yellow
Write-Host "Target: win-x64, self-contained, single-file" -ForegroundColor Cyan

dotnet publish $ProjectFile `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:DebugType=None `
    /p:DebugSymbols=false `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Publish failed" -ForegroundColor Red
    exit 1
}

Write-Host "Publish completed." -ForegroundColor Green
Write-Host ""

# Get published file info
$ExePath = Join-Path $PublishDir "OpenAIDictate.exe"

if (Test-Path $ExePath) {
    $FileInfo = Get-Item $ExePath
    $FileSizeMB = [math]::Round($FileInfo.Length / 1MB, 2)

    # Optional code signing
    $certPathEnv = $env:OPENAIDICTATE_CODESIGN_CERT_PATH
    $certBase64 = $env:OPENAIDICTATE_CODESIGN_CERT_BASE64
    $certPassword = $env:OPENAIDICTATE_CODESIGN_CERT_PASSWORD
    $timestampUrl = if ($env:OPENAIDICTATE_CODESIGN_TIMESTAMP) { $env:OPENAIDICTATE_CODESIGN_TIMESTAMP } else { "http://timestamp.digicert.com" }

    if ($certPathEnv -or $certBase64) {
        Write-Host "Signing executable with Authenticode..." -ForegroundColor Yellow
        $certificatePath = $certPathEnv
        $tempCert = $false

        if ($certBase64) {
            $certificatePath = Join-Path $env:TEMP "openaidictate_codesign.pfx"
            [IO.File]::WriteAllBytes($certificatePath, [Convert]::FromBase64String($certBase64))
            $tempCert = $true
        }

        try {
            $signtool = $env:SIGNTOOL_PATH
            if (-not $signtool) {
                $signtoolCmd = Get-Command signtool.exe -ErrorAction SilentlyContinue
                if ($signtoolCmd) {
                    $signtool = $signtoolCmd.Source
                } else {
                    $sdkRoots = @(
                        Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin",
                        Join-Path ${env:ProgramFiles} "Windows Kits\10\bin"
                    ) | Where-Object { Test-Path $_ }

                    foreach ($root in $sdkRoots) {
                        $candidate = Get-ChildItem $root -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending | ForEach-Object {
                            $toolPath = Join-Path $_.FullName "x64\signtool.exe"
                            if (Test-Path $toolPath) { return $toolPath }
                        }
                        if ($candidate) { $signtool = $candidate; break }
                    }
                }
            }

            if (-not $signtool -or -not (Test-Path $signtool)) {
                Write-Host "WARNING: signtool.exe not found. Skipping code signing." -ForegroundColor Yellow
            } else {
                $arguments = @('sign', '/fd', 'SHA256', '/td', 'SHA256', '/tr', $timestampUrl, '/d', 'OpenAIDictate', '/f', $certificatePath)
                if ($certPassword) { $arguments += @('/p', $certPassword) }
                $arguments += $ExePath

                & $signtool @arguments
                if ($LASTEXITCODE -ne 0) {
                    throw "signtool exited with code $LASTEXITCODE"
                }

                Write-Host "Code signing completed." -ForegroundColor Green
            }
        }
        finally {
            if ($tempCert -and (Test-Path $certificatePath)) {
                Remove-Item $certificatePath -Force
            }
        }
    }

    Write-Host "========================================" -ForegroundColor Green
    Write-Host "BUILD SUCCESSFUL" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Executable: $ExePath" -ForegroundColor Cyan
    Write-Host "Size: $FileSizeMB MB" -ForegroundColor Cyan
    Write-Host "Created: $($FileInfo.LastWriteTime)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "You can now deploy OpenAIDictate.exe to any Windows 10/11 x64 system." -ForegroundColor Yellow
    Write-Host "No installation required - just copy and run!" -ForegroundColor Yellow
    Write-Host ""

    # Optional: Open publish directory
    $OpenFolder = Read-Host "Open publish directory? (Y/N)"
    if ($OpenFolder -eq "Y" -or $OpenFolder -eq "y") {
        explorer $PublishDir
    }
} else {
    Write-Host "ERROR: Published executable not found at $ExePath" -ForegroundColor Red
    exit 1
}
