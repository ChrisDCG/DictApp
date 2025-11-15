# 🚀 Pre-Commit Setup - Quick Start

## Installation (5 Minuten)

### Windows
```powershell
.\scripts\setup-pre-commit.ps1
```

### Linux/Mac
```bash
./scripts/setup-pre-commit.sh
```

**Das war's!** Pre-Commit läuft jetzt automatisch bei jedem `git commit`.

## Was passiert bei einem Commit?

```
git commit -m "deine nachricht"

🔍 Running pre-commit checks...
  ✓ Trailing whitespace............Passed
  ✓ End of file fixer..............Passed
  ✓ Check YAML.....................Passed
  ✓ dotnet format..................Auto-Fixed ✨
  ✓ dotnet build...................Passed
  ✓ dotnet test....................Passed (2.3s)
  ✓ Roslyn Analyzers...............Passed
  ✓ Security scan..................Passed

✅ All checks passed! (3.8s)
```

## Features

### ✨ Auto-Fixing
- Code wird automatisch formatiert
- Using-Statements sortiert
- Whitespace bereinigt

### 🔍 Code-Analyse
- **8 verschiedene Analyzer** finden:
  - Bugs & Null-Reference-Exceptions
  - Security-Probleme (SQL Injection, XSS)
  - Performance-Issues
  - Code Smells
  - Async/Await Probleme

### 🧪 Automatische Tests
- Alle Tests laufen vor jedem Commit
- Code Coverage wird gemessen
- Failures blockieren den Commit

### ⚡ Schnell & Effizient
- Nur geänderte Dateien werden geprüft
- Incremental Build
- Durchschnitt: **3-8 Sekunden**

## Manuelle Nutzung

```bash
# Alle Checks ausführen
pre-commit run --all-files

# Nur Formatierung
pre-commit run dotnet-format --all-files

# Vollständiger Check mit Coverage
./scripts/pre-commit-full.sh  # Linux/Mac
.\scripts\pre-commit-full.ps1  # Windows
```

## Checks überspringen (Notfall)

```bash
# NUR in Notfällen!
git commit --no-verify -m "message"
```

## Mehr Infos

Siehe [docs/PRE_COMMIT_SETUP.md](docs/PRE_COMMIT_SETUP.md) für:
- Detaillierte Konfiguration
- Troubleshooting
- Performance-Tipps
- Analyzer-Regeln anpassen

## Analyzer im Detail

| Tool | Zweck | Regeln |
|------|-------|--------|
| StyleCop | Code-Style | 120+ |
| Roslynator | Best Practices | 500+ |
| SonarAnalyzer | Bugs & Security | 300+ |
| Meziantou | Performance | 150+ |
| SecurityCodeScan | Security | 30+ |
| AsyncFixer | Async/Await | 5+ |
| ErrorProne.NET | Common Bugs | 50+ |

**Gesamt**: Über **1150+ aktive Regeln** schützen deinen Code!

## IDE-Integration

### Visual Studio / Rider
- Analyzer zeigen Live-Feedback
- Quick-Fixes direkt verfügbar
- Gleiche Regeln wie Pre-Commit

### VS Code
Extensions installieren:
- C# Dev Kit
- EditorConfig for VS Code

```bash
# Öffne VS Code
code .
# Extensions werden automatisch vorgeschlagen
```

## Support

Probleme? Siehe:
1. [Troubleshooting](docs/PRE_COMMIT_SETUP.md#-troubleshooting)
2. Erstelle ein GitHub Issue
3. Check die Logs: `.git/hooks/pre-commit`

---

**Happy Coding!** 🎉

*Mit diesem Setup ist dein Code immer produktionsreif.*
