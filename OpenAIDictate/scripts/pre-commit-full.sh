#!/usr/bin/env bash
# Vollständiger Pre-Commit Check (Linux/Mac)
# Dieser Script führt alle Checks durch für maximale Qualität

set -e

echo "🔍 Running full pre-commit checks..."

# 1. Restore Dependencies
echo ""
echo "📦 Restoring dependencies..."
dotnet restore OpenAIDictate.sln --nologo

# 2. Format Check (mit Auto-Fix)
echo ""
echo "📝 Formatting code..."
dotnet format OpenAIDictate.sln --verbosity quiet

# 3. Build mit allen Analyzers
echo ""
echo "🔨 Building with analyzers..."
dotnet build OpenAIDictate.sln --configuration Debug --no-restore --nologo \
    /p:EnforceCodeStyleInBuild=true \
    /p:TreatWarningsAsErrors=false

# 4. Tests ausführen
echo ""
echo "🧪 Running tests..."
dotnet test OpenAIDictate.Tests/OpenAIDictate.Tests.csproj \
    --configuration Debug \
    --no-build \
    --nologo \
    --verbosity quiet \
    --logger "console;verbosity=minimal"

# 5. Code Coverage (optional)
echo ""
echo "📊 Collecting code coverage..."
dotnet test OpenAIDictate.Tests/OpenAIDictate.Tests.csproj \
    --configuration Debug \
    --no-build \
    --nologo \
    --collect:"XPlat Code Coverage" \
    --results-directory ./coverage \
    --verbosity quiet

# 6. Security Scan
echo ""
echo "🔒 Running security scan..."
# Security analyzers laufen bereits im Build

echo ""
echo "✅ All pre-commit checks passed!"
echo "📈 Code quality verified!"
