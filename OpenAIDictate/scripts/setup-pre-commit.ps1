# Setup-Script für Pre-Commit Hooks (Windows)
# Dieser Script installiert alle notwendigen Tools

$ErrorActionPreference = "Stop"

Write-Host "🚀 Setting up pre-commit environment..." -ForegroundColor Cyan

# 1. Python Check
Write-Host "`n🐍 Checking Python..." -ForegroundColor Yellow
try {
    $pythonVersion = python --version 2>&1
    Write-Host "✅ Found: $pythonVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Python not found! Please install Python 3.8+ from https://www.python.org/" -ForegroundColor Red
    exit 1
}

# 2. Install pre-commit
Write-Host "`n📦 Installing pre-commit..." -ForegroundColor Yellow
python -m pip install --upgrade pip --quiet
python -m pip install pre-commit --quiet
Write-Host "✅ pre-commit installed" -ForegroundColor Green

# 3. Install pre-commit hooks
Write-Host "`n🔧 Installing git hooks..." -ForegroundColor Yellow
pre-commit install --install-hooks
pre-commit install --hook-type commit-msg
Write-Host "✅ Git hooks installed" -ForegroundColor Green

# 4. .NET SDK Check
Write-Host "`n🔧 Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ Found .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET SDK not found! Please install .NET 8 SDK from https://dot.net/" -ForegroundColor Red
    exit 1
}

# 5. Restore NuGet packages (inkl. Analyzers)
Write-Host "`n📦 Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore OpenAIDictate.sln --nologo
Write-Host "✅ Packages restored" -ForegroundColor Green

# 6. Install additional tools
Write-Host "`n🛠️  Installing .NET tools..." -ForegroundColor Yellow
# dotnet format ist bereits in .NET 8 SDK enthalten
Write-Host "✅ .NET tools ready" -ForegroundColor Green

# 7. Run initial check
Write-Host "`n🧪 Running initial pre-commit check..." -ForegroundColor Yellow
pre-commit run --all-files || Write-Host "⚠️  Some checks failed - this is normal on first run" -ForegroundColor Yellow

Write-Host "`n✅ Pre-commit setup complete!" -ForegroundColor Green
Write-Host "`nℹ️  Usage:" -ForegroundColor Cyan
Write-Host "  - Hooks run automatically on 'git commit'" -ForegroundColor White
Write-Host "  - Manual run: pre-commit run --all-files" -ForegroundColor White
Write-Host "  - Fast check: ./scripts/pre-commit-fast.sh" -ForegroundColor White
Write-Host "  - Full check: ./scripts/pre-commit-full.ps1" -ForegroundColor White
Write-Host "`n🎉 Happy coding!" -ForegroundColor Green
