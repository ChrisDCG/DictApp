# OpenAIDictate - Publish Script
# This script creates a production-ready deployment package

param(
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "publish",

    [Parameter(Mandatory=$false)]
    [switch]$CreateZip,

    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "OpenAIDictate - Publish Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$projectFile = "OpenAIDictate\OpenAIDictate.csproj"
$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"

# Check if .NET SDK is available
Write-Host "Checking .NET SDK..." -ForegroundColor Cyan
try {
    $sdkCheck = & $dotnetPath --list-sdks 2>&1
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sdkCheck)) {
        Write-Host "[ERROR] .NET SDK not found!" -ForegroundColor Red
        Write-Host "Please run '.\setup.ps1' first to install the SDK." -ForegroundColor Yellow
        exit 1
    }
    Write-Host "[OK] .NET SDK found" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "[ERROR] Failed to check .NET SDK: $_" -ForegroundColor Red
    exit 1
}

# Check if project file exists
if (-not (Test-Path $projectFile)) {
    Write-Host "[ERROR] Project file not found: $projectFile" -ForegroundColor Red
    exit 1
}

# Create output directory
Write-Host "Preparing output directory..." -ForegroundColor Cyan
if (Test-Path $OutputPath) {
    Write-Host "Cleaning existing output directory..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -Path $OutputPath -ItemType Directory -Force | Out-Null
Write-Host "[OK] Output directory ready: $OutputPath" -ForegroundColor Green
Write-Host ""

# Publish application
Write-Host "Publishing application..." -ForegroundColor Cyan
Write-Host "Project: $projectFile" -ForegroundColor White
Write-Host "Target: Windows x64 (Self-Contained)" -ForegroundColor White
Write-Host "Output: $OutputPath" -ForegroundColor White
Write-Host ""

$publishArgs = @(
    "publish",
    $projectFile,
    "--configuration", "Release",
    "--framework", "net8.0-windows",
    "--runtime", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugType=none",
    "-p:DebugSymbols=false",
    "--output", $OutputPath
)

if ($Verbose) {
    $publishArgs += "--verbosity", "detailed"
}

try {
    & $dotnetPath $publishArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "[FAILED] Publish failed!" -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "[SUCCESS] Publish completed!" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "[ERROR] Publish error: $_" -ForegroundColor Red
    exit 1
}

# Analyze output
Write-Host "Analyzing output..." -ForegroundColor Cyan
$exePath = Join-Path $OutputPath "OpenAIDictate.exe"

if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    $sizeInMB = [math]::Round($fileInfo.Length / 1MB, 2)

    Write-Host "[OK] Executable created successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "Details:" -ForegroundColor White
    Write-Host "  File: OpenAIDictate.exe" -ForegroundColor White
    Write-Host "  Size: $sizeInMB MB" -ForegroundColor White
    Write-Host "  Path: $($fileInfo.FullName)" -ForegroundColor White
    Write-Host ""

    # Check for additional files
    $files = Get-ChildItem -Path $OutputPath -File
    Write-Host "Output contains $($files.Count) file(s):" -ForegroundColor White
    foreach ($file in $files) {
        $fileSizeKB = [math]::Round($file.Length / 1KB, 2)
        Write-Host "  - $($file.Name): $fileSizeKB KB" -ForegroundColor White
    }
    Write-Host ""
}
else {
    Write-Host "[ERROR] Executable not found in output directory!" -ForegroundColor Red
    exit 1
}

# Create README for distribution
Write-Host "Creating distribution README..." -ForegroundColor Cyan
$readmeContent = @"
# OpenAIDictate - Voice-to-Text Application

Version: 1.0
Built: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Platform: Windows 10/11 x64

## Installation

1. Extract all files to a folder of your choice
2. Run OpenAIDictate.exe
3. The application will appear in the system tray

## First-Time Setup

1. Right-click the tray icon and select "Settings"
2. Enter your OpenAI API key
   - Get your key from: https://platform.openai.com/api-keys
3. Configure your preferred hotkey (default: F5)
4. Select your preferred language and model
5. Click "Save"

## Usage

1. Press your configured hotkey to start recording
2. Speak your text
3. Press the hotkey again to stop recording
4. The transcription will be inserted at your cursor position

## Requirements

- Windows 10 (1809+) or Windows 11
- Internet connection (for OpenAI API)
- Microphone
- OpenAI API key

## Troubleshooting

### Application won't start
- Ensure Windows 10/11 x64
- Check antivirus/firewall settings
- Run as administrator (if needed)

### Hotkey doesn't work
- Check if another application is using the same hotkey
- Change the hotkey in Settings

### Microphone not detected
- Windows Settings > Privacy > Microphone
- Enable "Desktop apps can access your microphone"

### API errors
- Verify your API key in Settings
- Check your internet connection
- Ensure you have OpenAI API credits

## Data Storage

- Configuration: %APPDATA%\OpenAIDictate\config.json
- Logs: %APPDATA%\OpenAIDictate\logs\
- API keys are encrypted using Windows DPAPI

## Support

For issues and questions:
- Check WINDOWS_TEST_GUIDE.md
- Check WINDOWS_COMPATIBILITY_REPORT.md

## License

[Your License Here]

---
Built with .NET 8.0 for Windows
"@

$readmePath = Join-Path $OutputPath "README.txt"
Set-Content -Path $readmePath -Value $readmeContent -Encoding UTF8
Write-Host "[OK] README.txt created" -ForegroundColor Green
Write-Host ""

# Create ZIP archive if requested
if ($CreateZip) {
    Write-Host "Creating ZIP archive..." -ForegroundColor Cyan

    $version = "1.0.0"
    $date = Get-Date -Format "yyyyMMdd"
    $zipName = "OpenAIDictate-$version-$date-win-x64.zip"
    $zipPath = Join-Path (Get-Location) $zipName

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    try {
        Compress-Archive -Path "$OutputPath\*" -DestinationPath $zipPath -CompressionLevel Optimal
        Write-Host "[OK] ZIP archive created: $zipName" -ForegroundColor Green

        $zipInfo = Get-Item $zipPath
        $zipSizeMB = [math]::Round($zipInfo.Length / 1MB, 2)
        Write-Host "  Size: $zipSizeMB MB" -ForegroundColor White
        Write-Host "  Path: $($zipInfo.FullName)" -ForegroundColor White
        Write-Host ""
    }
    catch {
        Write-Host "[WARNING] Failed to create ZIP archive: $_" -ForegroundColor Yellow
        Write-Host ""
    }
}

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Publish Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[SUCCESS] Deployment package ready!" -ForegroundColor Green
Write-Host ""
Write-Host "Output location:" -ForegroundColor Yellow
Write-Host "  $(Resolve-Path $OutputPath)" -ForegroundColor White
Write-Host ""
Write-Host "Deployment files:" -ForegroundColor Yellow
Write-Host "  - OpenAIDictate.exe (self-contained, single-file)" -ForegroundColor White
Write-Host "  - README.txt (user guide)" -ForegroundColor White
Write-Host ""
Write-Host "The application is ready for distribution!" -ForegroundColor Green
Write-Host ""
Write-Host "To test the deployment package:" -ForegroundColor Yellow
Write-Host "  1. Copy the $OutputPath folder to another location" -ForegroundColor White
Write-Host "  2. Run OpenAIDictate.exe" -ForegroundColor White
Write-Host "  3. Follow the README.txt instructions" -ForegroundColor White
Write-Host ""

if ($CreateZip) {
    Write-Host "ZIP archive is ready for distribution." -ForegroundColor Green
    Write-Host ""
}
else {
    Write-Host "To create a ZIP archive, run:" -ForegroundColor Yellow
    Write-Host "  .\publish.ps1 -CreateZip" -ForegroundColor White
    Write-Host ""
}
