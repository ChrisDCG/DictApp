# OpenAIDictate - Build Script
# This script builds the project in Debug and Release configurations

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release", "Both")]
    [string]$Configuration = "Release",

    [Parameter(Mandatory=$false)]
    [switch]$Clean,

    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "OpenAIDictate - Build Script" -ForegroundColor Cyan
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

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    try {
        & $dotnetPath clean $projectFile
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[OK] Clean completed" -ForegroundColor Green
        }
        else {
            Write-Host "[WARNING] Clean had issues (non-critical)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "[WARNING] Clean failed: $_" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Build function
function Build-Project {
    param(
        [string]$Config
    )

    Write-Host "Building project ($Config)..." -ForegroundColor Cyan
    Write-Host "Project: $projectFile" -ForegroundColor White
    Write-Host "Configuration: $Config" -ForegroundColor White
    Write-Host ""

    $buildArgs = @(
        "build",
        $projectFile,
        "--configuration", $Config,
        "--framework", "net8.0-windows"
    )

    if ($Verbose) {
        $buildArgs += "--verbosity", "detailed"
    }

    try {
        & $dotnetPath $buildArgs

        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "[SUCCESS] Build completed: $Config" -ForegroundColor Green
            Write-Host ""

            # Show output location
            $outputPath = "OpenAIDictate\bin\$Config\net8.0-windows\win-x64"
            if (Test-Path $outputPath) {
                Write-Host "Output location:" -ForegroundColor Cyan
                Write-Host "  $outputPath" -ForegroundColor White

                $exePath = "$outputPath\OpenAIDictate.exe"
                if (Test-Path $exePath) {
                    $fileInfo = Get-Item $exePath
                    Write-Host "  OpenAIDictate.exe: $($fileInfo.Length / 1KB) KB" -ForegroundColor White
                }
            }
            Write-Host ""
            return $true
        }
        else {
            Write-Host ""
            Write-Host "[FAILED] Build failed: $Config" -ForegroundColor Red
            Write-Host ""
            return $false
        }
    }
    catch {
        Write-Host ""
        Write-Host "[FAILED] Build error: $_" -ForegroundColor Red
        Write-Host ""
        return $false
    }
}

# Build based on configuration parameter
$success = $true

if ($Configuration -eq "Both") {
    Write-Host "Building both Debug and Release configurations..." -ForegroundColor Yellow
    Write-Host ""

    if (-not (Build-Project -Config "Debug")) {
        $success = $false
    }

    if (-not (Build-Project -Config "Release")) {
        $success = $false
    }
}
else {
    if (-not (Build-Project -Config $Configuration)) {
        $success = $false
    }
}

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($success) {
    Write-Host "[SUCCESS] All builds completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  - Run '.\test.ps1' to execute unit tests" -ForegroundColor White
    Write-Host "  - Run '.\run.ps1' to start the application" -ForegroundColor White
    Write-Host "  - Run '.\publish.ps1' to create deployment package" -ForegroundColor White
    Write-Host ""
    exit 0
}
else {
    Write-Host "[FAILED] One or more builds failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  - Check error messages above" -ForegroundColor White
    Write-Host "  - Try running with -Verbose flag for more details" -ForegroundColor White
    Write-Host "  - Ensure all NuGet packages are restored" -ForegroundColor White
    Write-Host "  - Run '.\setup.ps1' to verify prerequisites" -ForegroundColor White
    Write-Host ""
    exit 1
}
