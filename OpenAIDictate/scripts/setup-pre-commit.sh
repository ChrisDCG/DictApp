#!/usr/bin/env bash
# Setup-Script für Pre-Commit Hooks (Linux/Mac)
# Dieser Script installiert alle notwendigen Tools

set -e

echo "🚀 Setting up pre-commit environment..."

# 1. Python Check
echo ""
echo "🐍 Checking Python..."
if command -v python3 &> /dev/null; then
    PYTHON_VERSION=$(python3 --version)
    echo "✅ Found: $PYTHON_VERSION"
    PYTHON_CMD=python3
elif command -v python &> /dev/null; then
    PYTHON_VERSION=$(python --version)
    echo "✅ Found: $PYTHON_VERSION"
    PYTHON_CMD=python
else
    echo "❌ Python not found! Please install Python 3.8+"
    exit 1
fi

# 2. Install pre-commit
echo ""
echo "📦 Installing pre-commit..."
$PYTHON_CMD -m pip install --upgrade pip --quiet
$PYTHON_CMD -m pip install pre-commit --quiet
echo "✅ pre-commit installed"

# 3. Install pre-commit hooks
echo ""
echo "🔧 Installing git hooks..."
pre-commit install --install-hooks
pre-commit install --hook-type commit-msg
echo "✅ Git hooks installed"

# 4. .NET SDK Check
echo ""
echo "🔧 Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✅ Found .NET SDK: $DOTNET_VERSION"
else
    echo "❌ .NET SDK not found! Please install .NET 8 SDK from https://dot.net/"
    exit 1
fi

# 5. Restore NuGet packages (inkl. Analyzers)
echo ""
echo "📦 Restoring NuGet packages..."
dotnet restore OpenAIDictate.sln --nologo
echo "✅ Packages restored"

# 6. Install additional tools
echo ""
echo "🛠️  Installing .NET tools..."
# dotnet format ist bereits in .NET 8 SDK enthalten
echo "✅ .NET tools ready"

# 7. Make scripts executable
echo ""
echo "🔐 Making scripts executable..."
chmod +x scripts/*.sh
echo "✅ Scripts are executable"

# 8. Run initial check
echo ""
echo "🧪 Running initial pre-commit check..."
pre-commit run --all-files || echo "⚠️  Some checks failed - this is normal on first run"

echo ""
echo "✅ Pre-commit setup complete!"
echo ""
echo "ℹ️  Usage:"
echo "  - Hooks run automatically on 'git commit'"
echo "  - Manual run: pre-commit run --all-files"
echo "  - Fast check: ./scripts/pre-commit-fast.sh"
echo "  - Full check: ./scripts/pre-commit-full.sh"
echo ""
echo "🎉 Happy coding!"
