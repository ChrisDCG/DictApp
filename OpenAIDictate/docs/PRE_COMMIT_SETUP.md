# Pre-Commit Testing & Auto-Debugging Setup

Dieses Projekt verwendet ein State-of-the-Art Pre-Commit Setup für maximale Code-Qualität ohne Zeitverlust.

## 🎯 Ziele

- **Automatisches Fixing**: Code wird automatisch formatiert
- **Frühe Fehlererkennung**: Probleme werden vor dem Commit erkannt
- **Zeitersparnis**: Nur relevante Checks laufen
- **Hohe Qualität**: Multiple Analyzer finden Bugs, Security-Issues und Code-Smells

## 🚀 Quick Start

### Windows

```powershell
# Setup (einmalig)
.\scripts\setup-pre-commit.ps1

# Danach bei jedem Commit automatisch aktiv!
```

### Linux/Mac

```bash
# Setup (einmalig)
./scripts/setup-pre-commit.sh

# Danach bei jedem Commit automatisch aktiv!
```

## 📋 Was wird geprüft?

### 1. Code Formatierung (Auto-Fix) ✨

- **Tool**: `dotnet format`
- **Verhalten**: Formatiert Code automatisch nach .editorconfig
- **Zeit**: < 1 Sekunde
- **Blockiert**: Nein (Auto-Fix)

### 2. Code-Analyse mit Roslyn Analyzers 🔍

Folgende Analyzer sind aktiv:

| Analyzer | Fokus | Regeln |
|----------|-------|--------|
| **StyleCop** | Code-Style | Naming, Layout, Dokumentation |
| **Roslynator** | Best Practices | 500+ Code-Verbesserungen |
| **SonarAnalyzer** | Bugs & Security | Code Smells, Vulnerabilities |
| **Meziantou.Analyzer** | Performance | .NET Best Practices |
| **SecurityCodeScan** | Security | SQL Injection, XSS, etc. |
| **AsyncFixer** | Async/Await | Deadlocks, Fire-and-Forget |
| **ErrorProne.NET** | Bugs | Common Mistakes |

**Beispiel-Findings**:
- Potential Null-Reference-Exceptions
- SQL Injection Vulnerabilities
- Async/Await Deadlocks
- Performance-Probleme
- Code Smells

### 3. Build Check 🔨

- **Tool**: `dotnet build`
- **Zeit**: 2-5 Sekunden (incremental)
- **Blockiert**: Ja, bei Compile-Fehlern

### 4. Unit Tests 🧪

- **Tool**: `dotnet test`
- **Umfang**: Alle Tests
- **Zeit**: Abhängig von Testanzahl
- **Blockiert**: Ja, bei Test-Failures

### 5. Code Coverage 📊

- **Tool**: `coverlet`
- **Output**: `./coverage/` Ordner
- **Integration**: Automatisch in Tests integriert

### 6. Security Checks 🔒

- **detect-secrets**: Verhindert Secrets im Code
- **SecurityCodeScan**: Findet Security-Vulnerabilities
- **Dependency Check**: (via Analyzer)

### 7. Datei-Checks ✅

- Trailing Whitespace entfernen
- End-of-File Newline sicherstellen
- YAML/JSON Syntax-Check
- Keine Large Files (> 5MB)
- Keine Merge-Conflicts

## ⚡ Performance-Optimierung

### Schneller Modus (Nur geänderte Dateien)

```bash
# Git Hook läuft automatisch nur auf staged files
git commit -m "message"
```

**Durchschnittliche Zeit**: 3-10 Sekunden

### Vollständiger Check

```powershell
# Windows
.\scripts\pre-commit-full.ps1

# Linux/Mac
./scripts/pre-commit-full.sh
```

**Empfohlen**: Vor jedem Push

### Manueller Check

```bash
# Alle Files
pre-commit run --all-files

# Nur ein spezifischer Hook
pre-commit run dotnet-format --all-files

# Nur staged files
pre-commit run
```

## 🔧 Konfiguration

### Analyzer-Regeln anpassen

**Dateien**:
- `.editorconfig` - Code-Style und Formatierung
- `.globalconfig` - Analyzer-Severities (error/warning/suggestion)
- `stylecop.json` - StyleCop-spezifische Settings
- `Directory.Build.props` - Zentrale Analyzer-Konfiguration

**Beispiel**: Regel deaktivieren

```ini
# In .globalconfig
dotnet_diagnostic.CA1062.severity = none
```

**Severity Levels**:
- `error` - Blockiert Build/Commit
- `warning` - Zeigt Warnung, blockiert nicht
- `suggestion` - IDE-Hinweis
- `silent` - Unsichtbar
- `none` - Deaktiviert

### Pre-Commit Hooks anpassen

Datei: `.pre-commit-config.yaml`

**Hook deaktivieren**:
```yaml
- id: dotnet-test-changed
  # ...
  exclude: ^$ # Deaktiviert durch unmögliches Pattern
```

**Hook überspringen** (temporär):
```bash
git commit --no-verify -m "message"
```

⚠️ **Warnung**: `--no-verify` sollte NUR in Notfällen verwendet werden!

## 🎓 Best Practices

### 1. Häufige Commits

Pre-Commit ist optimiert für kleine, häufige Commits:
- Weniger geänderte Dateien = schnellere Checks
- Frühere Fehlererkennung
- Bessere Git-History

### 2. Auto-Fix nutzen

Viele Probleme werden automatisch behoben:
- Code-Formatierung
- Using-Statements sortieren
- Trailing Whitespace

**Nach Auto-Fix**: Änderungen prüfen und re-stage
```bash
git add .
git commit -m "message"
```

### 3. IDE-Integration

**Visual Studio / Rider**:
- Analyzer laufen automatisch in der IDE
- Live-Feedback während dem Tippen
- Quick-Fixes direkt verfügbar

**VS Code**:
- Installiere "C# Dev Kit" Extension
- EditorConfig Extension (automatisch aktiv)

## 📊 Statistiken

Nach Setup sammelt das System automatisch Metriken:

- **Verhinderte Bugs**: Via Analyzer-Findings
- **Auto-Fixes**: Code-Formatierung, Style-Fixes
- **Code Coverage**: Trend-Analyse
- **Build-Zeit**: Performance-Monitoring

## 🐛 Troubleshooting

### "dotnet command not found"

**Lösung**: .NET 8 SDK installieren
- Windows: https://dot.net/
- Linux: `sudo apt install dotnet-sdk-8.0`
- Mac: `brew install dotnet-sdk`

### "pre-commit command not found"

**Lösung**: Pre-Commit Framework installieren
```bash
pip install pre-commit
pre-commit install
```

### "Hooks laufen nicht"

**Check**:
```bash
# Hooks installiert?
ls -la .git/hooks/pre-commit

# Pre-commit funktioniert?
pre-commit run --all-files
```

**Lösung**:
```bash
pre-commit install --install-hooks
```

### "Zu langsam"

**Optimierungen**:
1. Nur staged files committen
2. Tests parallelisieren (kommt automatisch)
3. Incremental Build nutzen (automatisch)

### "False Positives"

Analyzer-Regel anpassen in `.globalconfig`:

```ini
# Von error zu warning
dotnet_diagnostic.CA1234.severity = warning

# Komplett deaktivieren
dotnet_diagnostic.CA1234.severity = none
```

## 🔄 Updates

### Analyzer-Pakete aktualisieren

```bash
# Alle NuGet-Pakete
dotnet list package --outdated

# Analyzer-spezifisch
dotnet add package StyleCop.Analyzers --version <neue-version>
```

### Pre-Commit Hooks aktualisieren

```bash
# Automatisch (empfohlen)
pre-commit autoupdate

# Manuell in .pre-commit-config.yaml
# rev: v4.5.0 -> v4.6.0
```

## 📚 Weitere Ressourcen

- [.NET Code Analysis](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)
- [EditorConfig](https://editorconfig.org/)
- [Pre-Commit Framework](https://pre-commit.com/)
- [StyleCop](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
- [Roslynator](https://github.com/JosefPihrt/Roslynator)

## 🤝 Contributing

Verbesserungsvorschläge für das Pre-Commit Setup:

1. Issue erstellen mit Beschreibung
2. Änderungen testen mit `pre-commit run --all-files`
3. Pull Request erstellen

## ❓ Fragen?

Bei Problemen oder Fragen:
1. Check diese Dokumentation
2. Siehe Troubleshooting-Sektion
3. Erstelle ein Issue im Projekt-Repository
