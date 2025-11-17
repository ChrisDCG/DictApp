# Windows Test Guide - OpenAIDictate

## Voraussetzungen

### 1. .NET SDK Installation (KRITISCH!)
Aktuell ist nur die .NET Runtime installiert. Für Builds wird das SDK benötigt:

```powershell
# Pr\u00fcfen ob SDK installiert ist:
dotnet --list-sdks

# Falls leer, SDK installieren:
# Download von: https://dotnet.microsoft.com/download/dotnet/8.0
# Empfohlen: .NET 8.0 SDK
```

### 2. Aktuelle Runtime-Versionen
```
Microsoft.NETCore.App 6.0.36
Microsoft.NETCore.App 8.0.22
Microsoft.WindowsDesktop.App 6.0.36
Microsoft.WindowsDesktop.App 8.0.22
```

## Build-Prozess

### Vollst\u00e4ndiger Build
```powershell
# Im Projektverzeichnis:
cd OpenAIDictate
dotnet restore
dotnet build --configuration Release

# Oder Multi-Target Build:
dotnet build --configuration Release --framework net8.0-windows
```

### Publish (Standalone EXE)
```powershell
dotnet publish --configuration Release --framework net8.0-windows --runtime win-x64 --self-contained true -p:PublishSingleFile=true
```

Die fertige .exe befindet sich dann in:
```
OpenAIDictate\bin\Release\net8.0-windows\win-x64\publish\OpenAIDictate.exe
```

## Test-Szenarien

### 1. Anwendungsstart
- [ ] Doppelklick auf OpenAIDictate.exe
- [ ] Tray-Icon erscheint in der Taskleiste
- [ ] Keine Fehlermeldungen beim Start
- [ ] Settings-Dialog \u00f6ffnet sich beim ersten Start

### 2. Settings-Dialog
- [ ] \u00d6ffnen \u00fcber Tray-Icon Rechtsklick > "Settings"
- [ ] API Key eingeben k\u00f6nnen
- [ ] Model ausw\u00e4hlen (gpt-4o-mini, gpt-4o, whisper-1)
- [ ] Language ausw\u00e4hlen (de, en, auto)
- [ ] Hotkey konfigurieren (Standard: Ctrl+Alt+D)
- [ ] "Save" speichert Settings
- [ ] "Cancel" verwirft \u00c4nderungen

### 3. Secret Store (Windows Credential Manager)
- [ ] API Key wird sicher gespeichert
- [ ] \u00dcberpr\u00fcfen in: Systemsteuerung > Anmeldeinformationsverwaltung > Windows-Anmeldeinformationen
- [ ] Eintrag "OpenAIDictate_ApiKey" vorhanden
- [ ] API Key wird beim n\u00e4chsten Start wiederhergestellt

### 4. Hotkey-Funktionalit\u00e4t
- [ ] Hotkey registriert sich beim Start
- [ ] Hotkey funktioniert global (aus jeder Anwendung)
- [ ] Aufnahme startet beim Dr\u00fccken
- [ ] Aufnahme stoppt beim Loslassen
- [ ] Visuelles Feedback w\u00e4hrend der Aufnahme

### 5. Audio-Aufnahme
- [ ] Mikrofon wird erkannt
- [ ] Aufnahme wird gestartet
- [ ] Audio wird korrekt aufgezeichnet (WAV-Format)
- [ ] Tempor\u00e4re Dateien werden erstellt
- [ ] Tempor\u00e4re Dateien werden nach Verarbeitung gel\u00f6scht

### 6. Transkription
- [ ] Audio wird an OpenAI gesendet
- [ ] Transkription kommt zur\u00fcck
- [ ] Text wird in aktive Anwendung eingef\u00fcgt
- [ ] Fehlerbehandlung bei API-Problemen

### 7. Text-Injection
- [ ] Text wird korrekt in Notepad eingef\u00fcgt
- [ ] Text wird korrekt in Word eingef\u00fcgt
- [ ] Text wird korrekt in Browser eingef\u00fcgt
- [ ] Sonderzeichen werden korrekt \u00fcbertragen
- [ ] Umlaute (\u00e4, \u00f6, \u00fc, \u00df) funktionieren

### 8. Fehlerbehandlung
- [ ] Ung\u00fcltiger API Key: Fehlermeldung
- [ ] Kein Mikrofon: Fehlermeldung
- [ ] Kein Internet: Fehlermeldung
- [ ] API-Fehler: Benutzerfreundliche Meldung
- [ ] Fehler-Logs werden geschrieben

### 9. Logging
- [ ] Log-Datei wird erstellt in: %APPDATA%\OpenAIDictate\logs\
- [ ] Logs enthalten relevante Informationen
- [ ] Keine sensiblen Daten (API Keys) in Logs
- [ ] Log-Rotation funktioniert

### 10. Anwendungsbeendigung
- [ ] "Exit" im Tray-Men\u00fc beendet die Anwendung
- [ ] Alle Ressourcen werden freigegeben
- [ ] Hotkey wird abgemeldet
- [ ] Keine Prozesse bleiben h\u00e4ngen

## Unit Tests

```powershell
cd OpenAIDictate\tests\OpenAIDictate.Tests
dotnet test --configuration Release
```

### Erwartete Test-Ergebnisse
- [ ] Alle ServiceCollectionExtensions Tests bestehen
- [ ] Alle AudioPreprocessor Tests bestehen
- [ ] Keine Fehler oder Warnungen

## Bekannte Windows-spezifische Punkte

### 1. Windows Forms
- Projekt verwendet `UseWindowsForms`
- Kompatibel mit Windows 10/11
- Erfordert Desktop-Runtime

### 2. Windows Credential Manager
- SecretStore nutzt `CredentialManagement` NuGet Package
- Nur unter Windows verf\u00fcgbar
- Speichert Credentials sicher

### 3. Global Hotkeys
- Nutzt Windows API (user32.dll)
- Erfordert Administrator-Rechte NICHT
- P/Invoke Calls m\u00fcssen unter Windows funktionieren

### 4. Audio-Aufnahme
- NAudio ist Windows-kompatibel
- Nutzt WaveIn/WaveOut APIs
- Mikrofon-Zugriff muss erlaubt sein

### 5. Clipboard und SendInput
- TextInjector nutzt Windows-spezifische APIs
- SendInput f\u00fcr Tastatur-Simulation
- Clipboard f\u00fcr Copy-Paste

## Troubleshooting

### SDK nicht gefunden
```
Error: No .NET SDKs were found
L\u00f6sung: .NET 8.0 SDK installieren von https://dotnet.microsoft.com/download
```

### Runtime-Fehler
```
Error: Framework nicht gefunden
L\u00f6sung: .NET 8.0 Desktop Runtime installieren
```

### Hotkey funktioniert nicht
- Pr\u00fcfen ob anderes Programm gleichen Hotkey verwendet
- Als Administrator ausf\u00fchren (falls n\u00f6tig)
- Event Viewer pr\u00fcfen

### Mikrofon nicht gefunden
- Windows-Einstellungen > Datenschutz > Mikrofon
- Mikrofon-Zugriff f\u00fcr Desktop-Apps erlauben

### Text wird nicht eingef\u00fcgt
- Fokus muss auf Textfeld sein
- Einige Anwendungen blockieren SendInput
- Clipboard-Zugriff pr\u00fcfen

## Automatisierte Test-Checkliste

```powershell
# 1. SDK installiert?
dotnet --list-sdks

# 2. Build erfolgreich?
dotnet build OpenAIDictate/OpenAIDictate.csproj --configuration Release

# 3. Tests erfolgreich?
dotnet test OpenAIDictate/tests/OpenAIDictate.Tests/OpenAIDictate.Tests.csproj

# 4. Publish erfolgreich?
dotnet publish OpenAIDictate/OpenAIDictate.csproj --configuration Release --framework net8.0-windows

# 5. Anwendung startet?
Start-Process "OpenAIDictate\bin\Release\net8.0-windows\win-x64\publish\OpenAIDictate.exe"
```

## Manuelle Test-Durchf\u00fchrung

1. **Vorbereitungen**
   - .NET 8.0 SDK installieren
   - Build durchf\u00fchren
   - Alle Prozesse von OpenAIDictate beenden

2. **Anwendung starten**
   - .exe ausf\u00fchren
   - Tray-Icon pr\u00fcfen
   - Settings konfigurieren

3. **Funktionstest**
   - Notepad \u00f6ffnen
   - Cursor in Textfeld setzen
   - Hotkey dr\u00fccken und sprechen
   - Text pr\u00fcfen

4. **Abschluss**
   - Anwendung beenden
   - Logs pr\u00fcfen
   - Credential Manager pr\u00fcfen

## Status

- [x] Projekt-Struktur Windows-kompatibel
- [x] Dependencies Windows-kompatibel
- [ ] .NET SDK installiert (ERFORDERLICH!)
- [ ] Vollst\u00e4ndiger Build-Test
- [ ] Unit Tests ausgef\u00fchrt
- [ ] Manuelle Funktionstests
- [ ] Ende-zu-Ende Test
