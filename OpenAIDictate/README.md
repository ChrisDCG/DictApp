<p align="center">
  <img src="docs/media/openai-dictate-logo.svg" alt="OpenAIDictate logo" width="320" />
</p>

<h1 align="center">OpenAIDictate</h1>

<p align="center"><em>Professional-grade Windows dictation powered by OpenAI's GPT-4o transcription models.</em></p>

<p align="center">
  <strong>Sprache:</strong> Deutsch · <a href="README.en.md">English version</a>
</p>

---

## Inhaltsverzeichnis

1. [Überblick](#überblick)
2. [Funktionsumfang](#funktionsumfang)
3. [Architektur in Kürze](#architektur-in-kürze)
4. [Systemanforderungen](#systemanforderungen)
5. [Installation & Entwicklung](#installation--entwicklung)
6. [Konfiguration](#konfiguration)
7. [Benutzung](#benutzung)
8. [Qualitätssicherung](#qualitätssicherung)
9. [Testing](#testing)
10. [Deployment](#deployment)
11. [Dokumentation](#dokumentation)
12. [Sicherheit](#sicherheit)
13. [Fehlerbehebung](#fehlerbehebung)
14. [Beitragen](#beitragen)
15. [Lizenz](#lizenz)
16. [Repo-Aufräumen](#repo-aufräumen)

---

## Überblick

OpenAIDictate ist ein Windows-Tray-Client für hochwertige Sprach-zu-Text-Diktate. Das Tool nutzt `gpt-4o-transcribe` (oder kompatible Modelle) für maximale Genauigkeit, kombiniert mit lokaler Vorverarbeitung (Silero VAD) und einer vertraulichen Geheimnisverwaltung über Windows DPAPI.

## Funktionsumfang

- 🎙️ **High-End-Transkription** – GPT-4o Transcribe (Fallback auf gpt-4o-mini)
- 🪄 **Audio Preprocessing** – Silero VAD, Loudness-Normalisierung, optionales Postprocessing via GPT-4o-mini
- ⌨️ **Beliebige Hotkeys** – F5 standardmäßig, beliebige Kombinationen über die Einstellungen
- 🌍 **Mehrsprachige UI** – Deutsch/Englisch, inklusive dynamischer Umschaltung
- 🔒 **Sicherer Umgang mit Secrets** – DPAPI-Verschlüsselung, keine Klartextspeicherung
- 📈 **Monitoring** – Strukturiertes Logging (Serilog) plus optionale Token-Logprobabilities
- 🧰 **Bereit für Unternehmen** – Single-File Deployment, Code-Signing Workflow, erweiterbare Service-Schicht

## Architektur in Kürze

| Ebene            | Komponenten                                                                 |
|------------------|------------------------------------------------------------------------------|
| Presentation     | `SettingsForm` (Windows Forms), `AppTrayContext`                             |
| Services         | `ConfigService`, `SecretStore`, `SerilogLogger`, Audio-/OpenAI-Dienste       |
| Infrastruktur    | `ModelAssetManager` (Asset-Downloads, Checksummen), Tray-Integration        |
| Persistenz       | `%APPDATA%/OpenAIDictate/config.json`, `%APPDATA%/OpenAIDictate/logs`        |
| Externe Systeme  | OpenAI API, Silero ONNX-Modell-Repository                                   |

Weitere Details findest du in [`PROJECT_SUMMARY.md`](PROJECT_SUMMARY.md) und der [API-Dokumentation](docs/api.md).

## Systemanforderungen

- Windows 10/11 (x64)
- .NET 8 SDK für Entwicklung, .NET 8 Runtime im Deployment bereits enthalten
- OpenAI API Key
- Mikrofon bzw. Audio-Interface

## Installation & Entwicklung

### 1. Repository klonen

```powershell
git clone https://github.com/yourrepo/OpenAIDictate.git
cd OpenAIDictate
```

### 2. Abhängigkeiten installieren

```powershell
dotnet restore OpenAIDictate.sln
```

### 3. Build durchführen

```powershell
dotnet build OpenAIDictate.sln -c Release
```

### 4. Single-File-Build (optional)

```powershell
dotnet publish OpenAIDictate.csproj -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:EnableCompressionInSingleFile=true
```

Das Ergebnis liegt unter `bin/Release/net8.0-windows/win-x64/publish/OpenAIDictate.exe`.

## Konfiguration

### `.env`

Kopiere `.env.example` nach `.env` und trage deine Werte ein. Alternativ können die Variablen systemweit gesetzt werden.

| Variable                  | Beschreibung                                                        |
|---------------------------|---------------------------------------------------------------------|
| `OPENAI_API_KEY`          | Verwendeter OpenAI API Key (wird bevorzugt vor der verschlüsselten Konfig) |
| `OPENAI_TRANSCRIBE_MODEL` | Überschreibt das Standardmodell (`gpt-4o-transcribe`)               |
| `SILERO_VAD_MODEL_PATH`   | Optionaler Pfad zu einem bereits vorhandenen Silero-ONNX-Modell     |

### Einstellungen im UI

- Hotkey-Konfiguration (Funktionstaste oder Kombination)
- Glossar & Spracheinstellungen für bessere Modell-Performance
- Optionen für Postprocessing, VAD und Auto-Chunking

Persistente Konfigurationen werden in `%APPDATA%/OpenAIDictate/config.json` gespeichert.

## Benutzung

1. Anwendung starten (`OpenAIDictate.exe`).
2. Beim ersten Start API Key eingeben (verschlüsselte Ablage).
3. Icon erscheint im System Tray.
4. Hotkey drücken → Aufnahme startet. Erneut drücken → Transkription wird an Cursorposition eingefügt.
5. Status & Logs können über das Tray-Menü abgerufen werden.

## Qualitätssicherung

- **Formatter**: `.editorconfig` definiert das Projektformat. Prüfen mit `dotnet format`.
- **Linter**: .NET Analyzer sind aktiv, alle Warnungen müssen behoben werden.
- **Code-Reviews**: Jeder PR erfordert mindestens eine Maintainer-Freigabe.

## Testing

Unit-Tests befinden sich im Projekt `tests/OpenAIDictate.Tests` (xUnit). Die wichtigsten Szenarien prüfen u. a. die Priorisierung von Umgebungsvariablen in `ConfigService`.

```powershell
dotnet test OpenAIDictate.sln -c Release
```

Für UI-/Integrationstests können zusätzliche Projekte unter `tests/` angelegt werden.

## Deployment

- `build.ps1` automatisiert Restore, Build, Publish und optional Code-Signing.
- Assets wie das Silero VAD Modell werden beim ersten Start automatisch heruntergeladen und mit SHA-256 validiert.
- Für Offline-Deployments kann das Modell via PowerShell/Bash vorab geladen werden (siehe [DEPLOYMENT.md](DEPLOYMENT.md)).

## Dokumentation

- [`docs/api.md`](docs/api.md) – Übersicht der wichtigsten Services und Modelle.
- [`PROJECT_SUMMARY.md`](PROJECT_SUMMARY.md) – Architekturüberblick.
- [`DEPLOYMENT.md`](DEPLOYMENT.md) – Schritte für Produktion.
- [`CHANGELOG.md`](CHANGELOG.md) – Release-Historie.

## Sicherheit

- Keine Secrets im Repository – `.env` ist ausgeschlossen.
- API Keys werden per Windows DPAPI geschützt.
- TLS-geschützte Kommunikation mit der OpenAI API.
- Optionaler Code-Signing-Workflow über Umgebungsvariablen (`build.ps1`).

## Fehlerbehebung

| Problem                                   | Lösungsvorschlag                                                                 |
|-------------------------------------------|----------------------------------------------------------------------------------|
| Hotkey lässt sich nicht registrieren      | Andere Anwendung schließt/Hotkey in den Einstellungen ändern                    |
| Aufnahme startet nicht                    | Mikrofonberechtigungen prüfen, Default-Gerät kontrollieren                       |
| `401 Unauthorized` von der OpenAI API     | API Key prüfen (`OPENAI_API_KEY` oder verschlüsselte Ablage)                     |
| Download Silero-Model schlägt fehl        | Internetverbindung prüfen oder Modell manuell in `%APPDATA%/OpenAIDictate/Models` ablegen |

Weitere Tipps findest du außerdem in [DEPLOYMENT.md](DEPLOYMENT.md) und im Tray-Menü unter **Logs anzeigen**.

## Beitragen

Wir freuen uns über Beiträge! Lies bitte vorab die [Beitragsrichtlinien](CONTRIBUTING.md) und den [Code of Conduct](CODE_OF_CONDUCT.md). Pull Requests sollten Tests, Format-Checks (`dotnet format`) und aktualisierte Dokumentation enthalten.

## Lizenz

Dieses Projekt steht unter der [MIT Lizenz](LICENSE).

## Repo-Aufräumen

Das Skript `scripts/clean.ps1` automatisiert die wichtigsten Pflegearbeiten:

```powershell
pwsh ./scripts/clean.ps1
```

Ausgeführt werden `dotnet clean`, `dotnet format`, `dotnet test` (per `-SkipTests` optional überspringbar) und – sofern vorhanden – `pre-commit run --all-files` (abschaltbar via `-SkipPreCommit`). Damit bleibt das Repository frei von Build-Artefakten und die wichtigsten Checks laufen vor jedem Commit lokal.

---

<p align="center">Made with ❤️ for präzise Spracheingabe.</p>
