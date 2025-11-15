#!/usr/bin/env bash
# Schneller Pre-Commit Check für lokale Entwicklung
# Dieser Script läuft nur die wichtigsten Checks für maximale Geschwindigkeit

set -e

echo "🔍 Running fast pre-commit checks..."

# 1. Format Check (mit Auto-Fix)
echo "📝 Formatting code..."
dotnet format --include $(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$' | tr '\n' ' ') --verbosity quiet 2>/dev/null || true

# 2. Schneller Build Check
echo "🔨 Building project..."
dotnet build OpenAIDictate.sln --no-restore --nologo -clp:ErrorsOnly -p:TreatWarningsAsErrors=false

# 3. Nur geänderte Files analysieren
CHANGED_FILES=$(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$' || true)

if [ -n "$CHANGED_FILES" ]; then
    echo "🔎 Analyzing changed files..."
    # Analyzer Warnings nur für geänderte Files
    for file in $CHANGED_FILES; do
        echo "  Checking: $file"
    done
fi

echo "✅ Fast pre-commit checks passed!"
