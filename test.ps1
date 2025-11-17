# OpenAIDictate - Test Script
# This script runs all unit tests and generates a test report

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter(Mandatory=$false)]
    [switch]$Verbose,

    [Parameter(Mandatory=$false)]
    [switch]$Coverage
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "OpenAIDictate - Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$testProjectFile = "OpenAIDictate\tests\OpenAIDictate.Tests\OpenAIDictate.Tests.csproj"
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

# Check if test project exists
if (-not (Test-Path $testProjectFile)) {
    Write-Host "[ERROR] Test project not found: $testProjectFile" -ForegroundColor Red
    Write-Host ""
    Write-Host "Test project is missing. The application can still be built and run," -ForegroundColor Yellow
    Write-Host "but automated testing is not available." -ForegroundColor Yellow
    exit 1
}

# Build test project first
Write-Host "Building test project..." -ForegroundColor Cyan
try {
    & $dotnetPath build $testProjectFile --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to build test project" -ForegroundColor Red
        exit 1
    }
    Write-Host "[OK] Test project built successfully" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "[ERROR] Build error: $_" -ForegroundColor Red
    exit 1
}

# Run tests
Write-Host "Running unit tests..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host ""

$testArgs = @(
    "test",
    $testProjectFile,
    "--configuration", $Configuration,
    "--no-build",
    "--logger", "console;verbosity=detailed"
)

if ($Verbose) {
    $testArgs += "--verbosity", "detailed"
}

if ($Coverage) {
    Write-Host "Code coverage enabled..." -ForegroundColor Yellow
    $testArgs += "--collect", "XPlat Code Coverage"
}

try {
    & $dotnetPath $testArgs

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "Test Summary" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "[SUCCESS] All tests passed!" -ForegroundColor Green
        Write-Host ""

        if ($Coverage) {
            Write-Host "Code coverage report:" -ForegroundColor Yellow
            $coverageFiles = Get-ChildItem -Path "OpenAIDictate\tests\OpenAIDictate.Tests\TestResults" -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
            if ($coverageFiles) {
                Write-Host "Coverage file(s) generated:" -ForegroundColor White
                foreach ($file in $coverageFiles) {
                    Write-Host "  $($file.FullName)" -ForegroundColor White
                }
            }
            Write-Host ""
        }

        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  - Run '.\run.ps1' to start the application" -ForegroundColor White
        Write-Host "  - Run '.\publish.ps1' to create deployment package" -ForegroundColor White
        Write-Host "  - See WINDOWS_TEST_GUIDE.md for manual testing" -ForegroundColor White
        Write-Host ""
        exit 0
    }
    else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "Test Summary" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "[FAILED] Some tests failed!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please review the test output above for details." -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "[ERROR] Test execution failed: $_" -ForegroundColor Red
    Write-Host ""
    exit 1
}
