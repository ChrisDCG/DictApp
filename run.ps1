# OpenAIDictate - Run Script
# This script starts the OpenAIDictate application

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter(Mandatory=$false)]
    [switch]$Build
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "OpenAIDictate - Run Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$exePath = "OpenAIDictate\bin\$Configuration\net8.0-windows\win-x64\OpenAIDictate.exe"

# Build if requested
if ($Build) {
    Write-Host "Building project first..." -ForegroundColor Yellow
    & .\build.ps1 -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Build failed. Cannot run application." -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

# Check if executable exists
if (-not (Test-Path $exePath)) {
    Write-Host "[ERROR] Application not found: $exePath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please build the project first:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1 -Configuration $Configuration" -ForegroundColor White
    Write-Host ""
    Write-Host "Or run with -Build flag:" -ForegroundColor Yellow
    Write-Host "  .\run.ps1 -Configuration $Configuration -Build" -ForegroundColor White
    Write-Host ""
    exit 1
}

# Check for existing instances
Write-Host "Checking for existing instances..." -ForegroundColor Cyan
$existingProcess = Get-Process -Name "OpenAIDictate" -ErrorAction SilentlyContinue
if ($existingProcess) {
    Write-Host "[WARNING] OpenAIDictate is already running!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Existing process(es):" -ForegroundColor White
    foreach ($proc in $existingProcess) {
        Write-Host "  PID: $($proc.Id), Started: $($proc.StartTime)" -ForegroundColor White
    }
    Write-Host ""

    $kill = Read-Host "Do you want to kill existing instance(s)? (Y/N)"
    if ($kill -eq "Y" -or $kill -eq "y") {
        foreach ($proc in $existingProcess) {
            Write-Host "Stopping process $($proc.Id)..." -ForegroundColor Yellow
            Stop-Process -Id $proc.Id -Force
        }
        Write-Host "[OK] Existing instances stopped" -ForegroundColor Green
        Start-Sleep -Seconds 2
    }
    else {
        Write-Host "Aborting. Please close OpenAIDictate manually first." -ForegroundColor Yellow
        exit 1
    }
    Write-Host ""
}

# Start application
Write-Host "Starting OpenAIDictate..." -ForegroundColor Cyan
Write-Host "Executable: $exePath" -ForegroundColor White
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host ""

try {
    # Start the process
    $process = Start-Process -FilePath $exePath -PassThru -WindowStyle Hidden

    if ($process) {
        Write-Host "[SUCCESS] OpenAIDictate started!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Process ID: $($process.Id)" -ForegroundColor White
        Write-Host ""
        Write-Host "The application is running in the system tray." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "To use:" -ForegroundColor Cyan
        Write-Host "  1. Look for the tray icon in the taskbar (bottom-right)" -ForegroundColor White
        Write-Host "  2. Right-click the icon to access Settings" -ForegroundColor White
        Write-Host "  3. Enter your OpenAI API key in Settings" -ForegroundColor White
        Write-Host "  4. Configure your preferred hotkey (default: F5)" -ForegroundColor White
        Write-Host "  5. Press the hotkey to start recording" -ForegroundColor White
        Write-Host "  6. Speak, then press the hotkey again to transcribe" -ForegroundColor White
        Write-Host ""
        Write-Host "Logs are written to:" -ForegroundColor Cyan
        Write-Host "  $env:APPDATA\OpenAIDictate\logs\" -ForegroundColor White
        Write-Host ""
        Write-Host "To stop the application:" -ForegroundColor Cyan
        Write-Host "  - Right-click tray icon > Exit" -ForegroundColor White
        Write-Host "  - Or run: Stop-Process -Name OpenAIDictate" -ForegroundColor White
        Write-Host ""

        # Wait a moment to see if it crashes immediately
        Start-Sleep -Seconds 2

        if (-not $process.HasExited) {
            Write-Host "[OK] Application is running normally" -ForegroundColor Green
            Write-Host ""
        }
        else {
            Write-Host "[ERROR] Application exited immediately!" -ForegroundColor Red
            Write-Host "Exit code: $($process.ExitCode)" -ForegroundColor Red
            Write-Host ""
            Write-Host "Check logs for errors:" -ForegroundColor Yellow
            Write-Host "  $env:APPDATA\OpenAIDictate\logs\" -ForegroundColor White
            Write-Host ""
            exit 1
        }
    }
    else {
        Write-Host "[ERROR] Failed to start application" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "[ERROR] Failed to start application: $_" -ForegroundColor Red
    exit 1
}
