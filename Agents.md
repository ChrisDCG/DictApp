# Agents Guide für OpenAIDictate

Dieses Dokument richtet sich explizit an KI-gestützte Agents, die Aufgaben in diesem Repository übernehmen. Es bündelt alle Fakten, Leitplanken und Workflows, die ihr benötigt, um schnell, sicher und in hoher Qualität zu liefern.

## Ziel und Nutzen
- Kontext in unter 2 Minuten erfassen
- Relevante Dateien und Skripte finden, ohne das Repo zu durchsuchen
- Risiken früh erkennen (Audio, Sicherheit, Windows-spezifische APIs)
- Wiederholbare Arbeitsroutine für Analyse → Umsetzung → Tests → Handover

## Projekt-Essentials
- Produkt: **OpenAIDictate** – Windows-Tray-App für Profi-Spracherkennung mit `gpt-4o-transcribe`
- Runtime: `.NET 8`, WinForms, Single-File-Deployment (xcopy, keine Admin-Rechte)
- Kernpfad: `OpenAIDictate/` (alle C#-Quellen, `OpenAIDictate.sln`)
- Tests: `OpenAIDictate/tests/OpenAIDictate.Tests/` (xUnit + Coverlet)
- Build/Release-Skripte: `build.ps1`, `publish.ps1`, `scripts/pre-commit-*`

## Arbeitsprinzipien für Agents
1. **Deutsch bevorzugen**, Code-Kommentare/Identifiers Englisch lassen.
2. **Sicherheitsziele respektieren**: Keine Klartext-API-Keys, Audio bleibt RAM-only, Logging ohne Inhalte.
3. **State Machine schützen** (`AppTrayContext`): Nur definierte Übergänge Idle ↔ Recording ↔ Transcribing.
4. **Änderungen minimal-invasiv**: Vorhandene Services erweitern statt duplizieren; DI über `ServiceCollectionExtensions`.
5. **Dokumentation synchron halten**: Relevante Updates gleichzeitig in `README.md`, `PROJECT_SUMMARY.md`, `CHANGELOG.md`.

## Erkundungspfad
- `README.md` (root): Produktpitch, Features, Quick Start.
- `OpenAIDictate/PROJECT_SUMMARY.md`: Architekturdiagramme, Flows, Best Practices.
- `OpenAIDictate/DEPLOYMENT.md`: Enterprise-Deploy, wichtige Parameter.
- `docs/api.md`: Öffentliche API- und Prompting-Hinweise.
- `tests/OpenAIDictate.Tests/TESTING.md`: Teststrategie, Kategorien, Coverage-Ziele.
- `NEXT_STEPS.md`: Frische TODOs / Roadmap-Hinweise.

Beginne jede Session mit einem Blick auf `NEXT_STEPS.md` + Git-Status (`git status -sb`) um Konflikte zu vermeiden.

## Architektur- und Coding-Leitplanken
- **Serviceschicht** (`Services/*.cs`): Verwaltet Audio, Hotkeys, Transkription, Netzwerkstatus. Nutze vorhandene Interfaces (z.B. `ILogger`, `IMetricsService`) für neue Logik.
- **Modelle** (`Models/AppConfig.cs`, `AppState.cs`): Keine Businesslogik; nur DTOs/Enums.
- **Infrastruktur** (`Infrastructure/*.cs`): Querschnittsthemen wie Branding, Cursor-Overlay.
- **Konfiguration**: Persistiert in `%APPDATA%`; `ConfigService` kapselt Lesen/Schreiben, niemals direkt auf Files zugreifen.
- **Security**: `SecretStore` + DPAPI; API-Key nie loggen, nie im Klartext speichern.
- **Logging**: Verwende `ILogger` Interface → `SerilogLogger`. Keine Audio-/Textpayloads loggen.
- **Style**: `stylecop.json` aktiv. Bevorzugt `async/await`, `ConfigureAwait(false)` nicht nötig (WinForms). Namespace `OpenAIDictate.*`.
- **Nullability**: Projekt nutzt Nullable Reference Types; halte Compiler-Warnings bei nullability auf 0.

## Vorgehensmodell für Aufgaben
1. **Analyse**
   - User-Anforderung + Constraints extrahieren.
   - Betroffene Dateien identifizieren (grep, `rg`).
2. **Design**
   - Minimalen Eingriff wählen.
   - Edge Cases & Sicherheitsauswirkungen notieren.
3. **Implementierung**
   - Code in `OpenAIDictate/` bearbeiten.
   - Neue Services/Features über DI registrieren.
   - Strings lokalisierbar halten (`Resources/Strings*.resx`).
4. **Tests**
   - Unit Tests hinzufügen/aktualisieren (AAA-Pattern, FluentAssertions).
   - Bei API-/Netzwerkabhängigkeit Mocks verwenden.
5. **Validierung**
   - `dotnet build -c Release` im Projektordner.
   - `dotnet test` (siehe Abschnitt „Tests & Qualität“).
   - Optional: `dotnet format` oder `stylecop` falls verfügbar.
6. **Handover**
   - Änderungen kurz in der PR-Beschreibung (Problem, Lösung, Tests).
   - Relevante Dokumentation synchronisieren.

## Tests & Qualität
- **Standardlauf**: `cd OpenAIDictate && dotnet test`
- **Mit Coverage**: `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura`
- **Filter**: `dotnet test --filter "Category=Unit"` oder `"Category=Integration"`
- **Performanceziele**: Komplettes Unit-Pack <5s, vollständige Suite <2min, Coverage >90% (neuer Code nicht unter 85% fallen lassen).
- **Mocks**: Verwende `Moq`, `MockHttp`, eigene Test-Helper (`tests/.../TestHelpers`).
- **Skipped Tests**: Kategorien `RequiresApiKey`, `RequiresNetwork`, `Audio`, `UI`. Nur aktivieren, wenn Umgebung die Ressourcen bereitstellt.

## Build & Deployment
- Lokaler Build:  
  ```bash
  cd OpenAIDictate
  dotnet build -c Release
  ```
- Single-File Publish:  
  ```bash
  dotnet publish -c Release -r win-x64 --self-contained true \
    /p:PublishSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true \
    /p:EnableCompressionInSingleFile=true
  ```
- Output: `OpenAIDictate/bin/Release/net8.0-windows/win-x64/publish/OpenAIDictate.exe`
- Pre-commit-Hooks (optional, Linux/macOS): `scripts/pre-commit-fast.sh`, `scripts/pre-commit-full.sh`

## Häufige Stolperfallen
- **Hotkey-Konflikte**: Änderungen am `GlobalHotkeyService` immer mit Windows-spezifischen Edge Cases prüfen.
- **Clipboard/IME**: `TextInjector` muss Clipboard sichern/wiederherstellen → Integrationstests schwer; decke mit Unit-Tests & Mocks ab.
- **Audio-Hardware**: `AudioRecorder` Tests erfordern echtes Device → bei CI Mocks verwenden.
- **Offline-Modus**: `NetworkStatusService` blockiert Transkriptionen offline; neue Features müssen diesen Zustand sauber respektieren.
- **Large Streams**: Audio bleibt im RAM. Keine Kopien unnötig erzeugen, `MemoryStream.Position` korrekt setzen.
- **Lokalisierung**: UI-Strings gehören in `Resources/Strings.resx` + `Strings.de.resx`. Keine harten deutschen Texte im Code.
- **Security Audits**: Jede Änderung an `SecretStore`, `ConfigService`, `Logger` besonders prüfen (keine Klartext-Secrets, kein PII-Logging).

## Kommunikations- & Übergabestandards
- **Commit-Messages**: `<Bereich>: <Kurzbeschreibung>` (z.B. `Audio: Fix sample-rate clamp`).
- **PR-Beschreibung**: Problem → Lösung → Tests → Risiken.
- **Changelog**: Änderungen ab Release-Relevanz sofort in `OpenAIDictate/CHANGELOG.md` eintragen.
- **Docs pflegen**: Neue Features in `README.md`, `PROJECT_SUMMARY.md`, ggf. `docs/api.md`.

## Nützliche Kommandos (Cheatsheet)

| Ziel | Kommando |
|------|----------|
| Status prüfen | `git status -sb` |
| Formatierung | `dotnet format` (falls installiert) |
| StyleCop (falls integriert) | `dotnet build -p:RunAnalyzers=true` |
| Tests (alle) | `dotnet test` |
| Tests mit Filter | `dotnet test --filter "Category=Unit"` |
| Coverage HTML | `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura` |
| Cleanup (Windows) | `OpenAIDictate/scripts/clean.ps1` |

## Entscheidungsbaum für Agents
1. **Task verstanden?** → sonst Rückfrage an User.
2. **Bestehende Lösung vorhanden?** → Reuse > Rewrite.
3. **Änderung sicherheitsrelevant?** → Review `Security` Abschnitt im README + DPAPI-Code.
4. **UI-Änderung?** → Strings in ResX, Lokalisierungen prüfen.
5. **Feature toggle nötig?** → `AppConfig` + `SettingsForm` erweitern, Tests ergänzen.
6. **Neuer Service?** → Interface + Tests + Registrierung in `ServiceCollectionExtensions`.

## Abschluss
Folge diesem Leitfaden strikt, dokumentiere jeden Schritt, und stelle sicher, dass jede Änderung reproduzierbar getestet wurde. Ein „perfekter Agent“ liefert nachvollziehbare Commits, hohe Testabdeckung, respektiert Security-Vorgaben und hält die Dokumentation synchron.
