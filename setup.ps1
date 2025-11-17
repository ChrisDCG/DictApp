# OpenAIDictate - Setup Script
# This script prepares the development environment and installs all prerequisites

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "OpenAIDictate - Setup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if running as administrator
function Test-Administrator {
    $currentUser = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Function to download file
function Download-File {
    param (
        [string]$Url,
        [string]$OutputPath
    )

    Write-Host "Downloading from $Url..." -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri $Url -OutFile $OutputPath -UseBasicParsing
        Write-Host "Download complete: $OutputPath" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "Download failed: $_" -ForegroundColor Red
        return $false
    }
}

# Check if running as administrator
if (-not (Test-Administrator)) {
    Write-Host "WARNING: Not running as administrator. Some operations may fail." -ForegroundColor Yellow
    Write-Host "Consider running this script as administrator." -ForegroundColor Yellow
    Write-Host ""
}

# Step 1: Check .NET SDK
Write-Host "Step 1: Checking .NET SDK..." -ForegroundColor Cyan
$sdkCheck = & "C:\Program Files\dotnet\dotnet.exe" --list-sdks 2>$null
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sdkCheck)) {
    Write-Host "[MISSING] .NET SDK not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "You need to install the .NET 8.0 SDK:" -ForegroundColor Yellow
    Write-Host "1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
    Write-Host "2. Download: '.NET SDK 8.0.x' (NOT just Runtime!)" -ForegroundColor White
    Write-Host "3. Run the installer" -ForegroundColor White
    Write-Host "4. Restart this script after installation" -ForegroundColor White
    Write-Host ""

    $download = Read-Host "Do you want me to download the .NET 8.0 SDK installer? (Y/N)"
    if ($download -eq "Y" -or $download -eq "y") {
        $sdkUrl = "https://download.visualstudio.microsoft.com/download/pr/b280d97f-25a9-4ab7-8a12-8291aa3af117/a7d8f93b5fc5775a77f0e3d5288c6c6b/dotnet-sdk-8.0.404-win-x64.exe"
        $installerPath = "$env:TEMP\dotnet-sdk-8.0-installer.exe"

        if (Download-File -Url $sdkUrl -OutputPath $installerPath) {
            Write-Host ""
            Write-Host "Starting installer..." -ForegroundColor Green
            Start-Process -FilePath $installerPath -Wait

            Write-Host ""
            Write-Host "Please restart this script after installation completes." -ForegroundColor Yellow
            pause
            exit
        }
    }
    else {
        Write-Host ""
        Write-Host "Please install .NET SDK manually and restart this script." -ForegroundColor Yellow
        pause
        exit
    }
}
else {
    Write-Host "[OK] .NET SDK found:" -ForegroundColor Green
    Write-Host $sdkCheck
}
Write-Host ""

# Step 2: Check .NET Runtimes
Write-Host "Step 2: Checking .NET Runtimes..." -ForegroundColor Cyan
$runtimeCheck = & "C:\Program Files\dotnet\dotnet.exe" --list-runtimes 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] .NET Runtimes found:" -ForegroundColor Green
    Write-Host $runtimeCheck
}
else {
    Write-Host "[WARNING] Could not check runtimes" -ForegroundColor Yellow
}
Write-Host ""

# Step 3: Verify project structure
Write-Host "Step 3: Verifying project structure..." -ForegroundColor Cyan
$projectFile = "OpenAIDictate\OpenAIDictate.csproj"
$testProjectFile = "OpenAIDictate\tests\OpenAIDictate.Tests\OpenAIDictate.Tests.csproj"

if (Test-Path $projectFile) {
    Write-Host "[OK] Main project found: $projectFile" -ForegroundColor Green
}
else {
    Write-Host "[ERROR] Main project not found: $projectFile" -ForegroundColor Red
    pause
    exit
}

if (Test-Path $testProjectFile) {
    Write-Host "[OK] Test project found: $testProjectFile" -ForegroundColor Green
}
else {
    Write-Host "[WARNING] Test project not found: $testProjectFile" -ForegroundColor Yellow
}
Write-Host ""

# Step 4: Restore NuGet packages
Write-Host "Step 4: Restoring NuGet packages..." -ForegroundColor Cyan
try {
    & "C:\Program Files\dotnet\dotnet.exe" restore $projectFile
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] NuGet packages restored successfully" -ForegroundColor Green
    }
    else {
        Write-Host "[ERROR] Failed to restore NuGet packages" -ForegroundColor Red
    }
}
catch {
    Write-Host "[ERROR] Failed to restore NuGet packages: $_" -ForegroundColor Red
}
Write-Host ""

# Step 5: Check Windows Features
Write-Host "Step 5: Checking Windows Features..." -ForegroundColor Cyan

# Check if microphone is available
try {
    Add-Type -AssemblyName System.Windows.Forms
    Write-Host "[OK] Windows Forms available" -ForegroundColor Green
}
catch {
    Write-Host "[WARNING] Windows Forms may not be available" -ForegroundColor Yellow
}

# Check audio devices
Write-Host "Checking audio devices..." -ForegroundColor White
$audioDevices = Get-WmiObject -Class Win32_SoundDevice -ErrorAction SilentlyContinue
if ($audioDevices) {
    Write-Host "[OK] Audio devices found:" -ForegroundColor Green
    foreach ($device in $audioDevices) {
        Write-Host "  - $($device.Name)" -ForegroundColor White
    }
}
else {
    Write-Host "[WARNING] No audio devices detected" -ForegroundColor Yellow
}
Write-Host ""

# Step 6: Check network connectivity
Write-Host "Step 6: Checking network connectivity..." -ForegroundColor Cyan
try {
    $ping = Test-Connection -ComputerName "api.openai.com" -Count 1 -Quiet
    if ($ping) {
        Write-Host "[OK] OpenAI API is reachable" -ForegroundColor Green
    }
    else {
        Write-Host "[WARNING] Cannot reach OpenAI API (check firewall/internet)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "[WARNING] Network check failed: $_" -ForegroundColor Yellow
}
Write-Host ""

# Step 7: Create config directory
Write-Host "Step 7: Preparing config directory..." -ForegroundColor Cyan
$appDataPath = "$env:APPDATA\OpenAIDictate"
if (-not (Test-Path $appDataPath)) {
    New-Item -Path $appDataPath -ItemType Directory -Force | Out-Null
    Write-Host "[OK] Config directory created: $appDataPath" -ForegroundColor Green
}
else {
    Write-Host "[OK] Config directory exists: $appDataPath" -ForegroundColor Green
}
Write-Host ""

# Step 8: Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Setup completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run '.\build.ps1' to build the project" -ForegroundColor White
Write-Host "2. Run '.\test.ps1' to run unit tests" -ForegroundColor White
Write-Host "3. Run '.\run.ps1' to start the application" -ForegroundColor White
Write-Host ""
Write-Host "For detailed testing instructions, see:" -ForegroundColor Yellow
Write-Host "  - WINDOWS_TEST_GUIDE.md" -ForegroundColor White
Write-Host "  - WINDOWS_COMPATIBILITY_REPORT.md" -ForegroundColor White
Write-Host ""

pause
