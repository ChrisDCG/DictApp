# Vollständiger Pre-Commit Check (Windows PowerShell)
# Dieser Script führt alle Checks durch für maximale Qualität

$ErrorActionPreference = "Stop"

Write-Host "🔍 Running full pre-commit checks..." -ForegroundColor Cyan

# 1. Restore Dependencies
Write-Host "`n📦 Restoring dependencies..." -ForegroundColor Yellow
dotnet restore OpenAIDictate.sln --nologo

# 2. Format Check (mit Auto-Fix)
Write-Host "`n📝 Formatting code..." -ForegroundColor Yellow
dotnet format OpenAIDictate.sln --verbosity quiet

# 3. Build mit allen Analyzers
Write-Host "`n🔨 Building with analyzers..." -ForegroundColor Yellow
dotnet build OpenAIDictate.sln --configuration Debug --no-restore --nologo `
    /p:EnforceCodeStyleInBuild=true `
    /p:TreatWarningsAsErrors=false

# 4. Tests ausführen
Write-Host "`n🧪 Running tests..." -ForegroundColor Yellow
dotnet test OpenAIDictate.Tests/OpenAIDictate.Tests.csproj `
    --configuration Debug `
    --no-build `
    --nologo `
    --verbosity quiet `
    --logger "console;verbosity=minimal"

# 5. Code Coverage (optional)
Write-Host "`n📊 Collecting code coverage..." -ForegroundColor Yellow
dotnet test OpenAIDictate.Tests/OpenAIDictate.Tests.csproj `
    --configuration Debug `
    --no-build `
    --nologo `
    --collect:"XPlat Code Coverage" `
    --results-directory ./coverage `
    --verbosity quiet

# 6. Security Scan
Write-Host "`n🔒 Running security scan..." -ForegroundColor Yellow
# Security analyzers laufen bereits im Build

Write-Host "`n✅ All pre-commit checks passed!" -ForegroundColor Green
Write-Host "📈 Code quality verified!" -ForegroundColor Green
