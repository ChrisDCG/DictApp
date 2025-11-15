# OpenAIDictate - Deployment Guide

Dieses Dokument beschreibt die vollständige Bereitstellung von OpenAIDictate in Unternehmensumgebungen.

## Inhaltsverzeichnis

1. [Systemanforderungen](#systemanforderungen)
2. [Build-Prozess](#build-prozess)
3. [Deployment-Optionen](#deployment-optionen)
4. [Erstmalige Konfiguration](#erstmalige-konfiguration)
5. [Unternehmensweite Bereitstellung](#unternehmensweite-bereitstellung)
6. [Sicherheitshinweise](#sicherheitshinweise)
7. [Fehlerbehebung](#fehlerbehebung)

---

## Systemanforderungen

### Zielrechner (Endbenutzer)

**Betriebssystem**:
- Windows 10 (Build 1809 oder höher)
- Windows 11 (alle Versionen)
- Architektur: x64

**Hardware**:
- Prozessor: x64-kompatibel (Intel/AMD)
- RAM: Mindestens 4 GB (8 GB empfohlen)
- Festplatte: 50 MB freier Speicher
- Mikrofon: Beliebiges USB- oder eingebautes Mikrofon

**Netzwerk**:
- Internetzugang für OpenAI API (HTTPS, Port 443)
- Optional: Proxy-Unterstützung über Windows-Systemeinstellungen

**Berechtigungen**:
- ✅ **KEINE** Administratorrechte erforderlich
- ✅ Standard-Benutzerkontext ausreichend
- ✅ Schreibrechte in `%APPDATA%` (standardmäßig vorhanden)

### Build-Rechner (Entwickler)

- Windows 10/11 x64
- .NET 8 SDK oder höher ([download](https://dotnet.microsoft.com/download))
- PowerShell 5.1 oder höher
- Optional: Visual Studio 2022 (für Entwicklung)

---

## Build-Prozess

### Option 1: PowerShell-Build-Skript (empfohlen)

```powershell
cd OpenAIDictate
.\build.ps1 -Configuration Release -Clean
```

**Parameter**:
- `-Configuration`: `Debug` oder `Release` (Standard: `Release`)
- `-Clean`: Löscht vorherige Builds vor neuem Build

**Ausgabe**:
```
bin\Release\net8.0-windows\win-x64\publish\OpenAIDictate.exe
```

### Option 2: Manuelle Kommandozeile

```powershell
# 1. Restore
dotnet restore OpenAIDictate.csproj

# 2. Build
dotnet build OpenAIDictate.csproj -c Release

# 3. Publish (Single-File-EXE)
dotnet publish OpenAIDictate.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true `
  /p:DebugType=None `
  /p:DebugSymbols=false
```

### Option 3: Visual Studio

1. Öffnen Sie `OpenAIDictate.sln`
2. Rechtsklick auf Projekt → **Veröffentlichen**
3. Profil: **Ordner**
4. Zielframework: **net8.0-windows**
5. Runtime: **win-x64**
6. Deployment-Modus: **Eigenständig**
7. **Veröffentlichen** klicken

---

## Deployment-Optionen

### Option A: Netzlaufwerk (empfohlen für Unternehmen)

**Vorteile**:
- Zentrale Verwaltung
- Einfache Updates (einmalig ersetzen)
- Keine lokale Installation

**Schritte**:

1. **Bereitstellen**:
   ```powershell
   # Auf Fileserver kopieren
   Copy-Item "bin\Release\net8.0-windows\win-x64\publish\OpenAIDictate.exe" `
             "\\fileserver\share\Tools\OpenAIDictate\OpenAIDictate.exe"
   ```

2. **Verknüpfung erstellen** (optional):
   ```powershell
   # PowerShell-Skript für automatische Verknüpfung
   $WshShell = New-Object -ComObject WScript.Shell
   $Shortcut = $WshShell.CreateShortcut("$env:APPDATA\Microsoft\Windows\Start Menu\Programs\OpenAIDictate.lnk")
   $Shortcut.TargetPath = "\\fileserver\share\Tools\OpenAIDictate\OpenAIDictate.exe"
   $Shortcut.WorkingDirectory = "\\fileserver\share\Tools\OpenAIDictate"
   $Shortcut.Save()
   ```

3. **Autostart** (optional):
   ```powershell
   # Verknüpfung in Autostart-Ordner kopieren
   Copy-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\OpenAIDictate.lnk" `
             "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\"
   ```

### Option B: Lokale Installation (Benutzerprofil)

**Vorteile**:
- Funktioniert offline (nach Konfiguration)
- Schnellerer Start

**Schritte**:

```powershell
# 1. Zielverzeichnis erstellen
$TargetDir = "$env:LOCALAPPDATA\Programs\OpenAIDictate"
New-Item -ItemType Directory -Path $TargetDir -Force

# 2. EXE kopieren
Copy-Item "OpenAIDictate.exe" "$TargetDir\OpenAIDictate.exe"

# 3. Verknüpfung erstellen
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:APPDATA\Microsoft\Windows\Start Menu\Programs\OpenAIDictate.lnk")
$Shortcut.TargetPath = "$TargetDir\OpenAIDictate.exe"
$Shortcut.WorkingDirectory = $TargetDir
$Shortcut.Save()
```

### Option C: USB-Stick (mobil)

**Vorteile**:
- Portabel
- Keine Netzwerkabhängigkeit
- Funktioniert auf jedem PC

**Schritte**:

1. Kopieren Sie `OpenAIDictate.exe` auf USB-Stick
2. Erstellen Sie `config.json` mit Ihrer Konfiguration (siehe unten)
3. **Wichtig**: Setzen Sie `OPENAI_API_KEY` als Umgebungsvariable (NICHT in config.json auf USB-Stick speichern!)

---

## Erstmalige Konfiguration

### Benutzer-spezifisch

Beim ersten Start fordert OpenAIDictate zur Eingabe des OpenAI API-Schlüssels auf.

**Automatisierte Konfiguration** (für IT-Admins):

```powershell
# Option 1: Umgebungsvariable (empfohlen für Unternehmen)
[System.Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "sk-...", "User")

# Option 2: Vorausgefüllte Config erstellen
$ConfigDir = "$env:APPDATA\OpenAIDictate"
New-Item -ItemType Directory -Path $ConfigDir -Force

$Config = @{
    model = "gpt-4o-transcribe"
    language = "de"
    maxRecordingMinutes = 10
    glossary = "Bundesgerichtshof, Schadensersatz, BGB"
    enablePostProcessing = $true
    enableVAD = $true
    silenceThresholdDb = -20.0
} | ConvertTo-Json

$Config | Out-File "$ConfigDir\config.json" -Encoding UTF8
```

### Unternehmensweite Standardkonfiguration

**config.json** (Vorlage):

```json
{
  "model": "gpt-4o-transcribe",
  "hotkeyGesture": "F5",
  "maxRecordingMinutes": 10,
  "language": "de",
  "glossary": "Firmenname GmbH, Produktname X, §§ 280, 241 BGB, Compliance",
  "enablePostProcessing": true,
  "enableVAD": true,
  "silenceThresholdDb": -20.0
}
```

**Deployment-Skript** (PowerShell):

```powershell
# deploy-openaidictate.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey,

    [Parameter(Mandatory=$false)]
    [string]$Glossary = "Standard, Fachbegriffe, Produktnamen"
)

# 1. Umgebungsvariable setzen (DPAPI-geschützt)
[System.Environment]::SetEnvironmentVariable("OPENAI_API_KEY", $ApiKey, "User")

# 2. Config erstellen
$ConfigDir = "$env:APPDATA\OpenAIDictate"
New-Item -ItemType Directory -Path $ConfigDir -Force

$Config = @{
    model = "gpt-4o-transcribe"
    language = "de"
    maxRecordingMinutes = 10
    glossary = $Glossary
    enablePostProcessing = $true
    enableVAD = $true
} | ConvertTo-Json

$Config | Out-File "$ConfigDir\config.json" -Encoding UTF8

Write-Host "OpenAIDictate erfolgreich konfiguriert!" -ForegroundColor Green
```

---

## Unternehmensweite Bereitstellung

### Szenario: 100+ Benutzer, zentrale Verwaltung

**Architektur**:
```
Fileserver (\\server\tools\OpenAIDictate\)
  ├── OpenAIDictate.exe         (einmalig aktualisiert)
  └── default-config.json        (Vorlage)

Benutzer-PC (pro User):
  %APPDATA%\OpenAIDictate\
    ├── config.json              (benutzer-spezifisch)
    └── logs\                    (optional)
```

**GPO-basiertes Deployment** (Group Policy):

1. **Logon-Skript** (`deploy-openaidictate.ps1`):

```powershell
# GPO Logon Script
$SourceExe = "\\fileserver\share\Tools\OpenAIDictate\OpenAIDictate.exe"
$TargetDir = "$env:LOCALAPPDATA\Programs\OpenAIDictate"

# Erstelle Verzeichnis
New-Item -ItemType Directory -Path $TargetDir -Force -ErrorAction SilentlyContinue

# Kopiere EXE (nur wenn neuer)
if (-not (Test-Path "$TargetDir\OpenAIDictate.exe") -or
    ((Get-Item $SourceExe).LastWriteTime -gt (Get-Item "$TargetDir\OpenAIDictate.exe").LastWriteTime)) {
    Copy-Item $SourceExe "$TargetDir\OpenAIDictate.exe" -Force
}

# Erstelle Startmenü-Verknüpfung
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:APPDATA\Microsoft\Windows\Start Menu\Programs\OpenAIDictate.lnk")
$Shortcut.TargetPath = "$TargetDir\OpenAIDictate.exe"
$Shortcut.Save()

# Optional: Autostart
# Copy-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\OpenAIDictate.lnk" `
#           "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\" -Force
```

2. **GPO-Einstellungen**:
   - Computer Configuration → Policies → Windows Settings → Scripts → Startup
   - Fügen Sie `deploy-openaidictate.ps1` hinzu

3. **API-Key-Verwaltung**:

**Option A**: Zentrale Umgebungsvariable (GPO):
- User Configuration → Preferences → Windows Settings → Environment
- Variable: `OPENAI_API_KEY`
- Wert: `sk-...` (Organisations-API-Key)

**Option B**: Benutzer gibt selbst Key ein (beim ersten Start)

---

## Sicherheitshinweise

### API-Key-Schutz

**✅ Empfohlen**:
- Umgebungsvariable `OPENAI_API_KEY` (User-Scope)
- DPAPI-verschlüsselt in `config.json` (automatisch beim ersten Start)

**❌ Nicht empfohlen**:
- Klartext-API-Keys in Skripten
- API-Keys auf Netzlaufwerken
- API-Keys in USB-Stick-Configs

### Firewall / Proxy

OpenAIDictate benötigt Zugriff auf:
- `https://api.openai.com` (Port 443)

**Proxy-Konfiguration**:
OpenAIDictate nutzt die Windows-System-Proxy-Einstellungen automatisch.

```powershell
# System-Proxy setzen (falls erforderlich)
netsh winhttp set proxy proxy-server="proxy.firma.de:8080"
```

### AppLocker / Code Signing

**AppLocker-Regel** (für Unternehmen):

```xml
<FilePublisherRule Id="..." Name="OpenAIDictate"
  Action="Allow" UserOrGroupSid="S-1-1-0">
  <Conditions>
    <FilePublisherCondition
      PublisherName="*"
      ProductName="OpenAIDictate"
      BinaryName="OpenAIDictate.exe">
      <BinaryVersionRange LowSection="1.1.0.0" HighSection="*" />
    </FilePublisherCondition>
  </Conditions>
</FilePublisherRule>
```

**Code-Signing** (optional, empfohlen für Produktion):

```powershell
# Signieren mit Unternehmenszertifikat
$Cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert
Set-AuthenticodeSignature -FilePath "OpenAIDictate.exe" -Certificate $Cert -TimestampServer "http://timestamp.digicert.com"
```

---

## Fehlerbehebung

### "Failed to register hotkey"

**Ursache**: Die gewählte Geste ist bereits von einer anderen Anwendung reserviert.

**Lösung**:
1. Schließen Sie Anwendungen, die denselben Hotkey verwenden
2. Öffnen Sie **Einstellungen → Hotkey** und wählen Sie eine freie Kombination (z.B. `Ctrl+Shift+F10`)

### "No recording device found"

**Ursache**: Kein Mikrofon verfügbar oder deaktiviert.

**Lösung**:
1. Windows-Einstellungen → Datenschutz → Mikrofon → **Zugriff erlauben**
2. Systemsteuerung → Sound → Aufnahme → Standard-Mikrofon festlegen

### "OpenAI API error 401"

**Ursache**: Ungültiger API-Key.

**Lösung**:
1. Überprüfen Sie `OPENAI_API_KEY` Umgebungsvariable
2. Oder: Löschen Sie `%APPDATA%\OpenAIDictate\config.json` → Neustart → Key erneut eingeben

### "Transcription timeout"

**Ursache**: Langsame Internetverbindung oder zu lange Aufnahme.

**Lösung**:
1. Kürzere Aufnahmen (< 2 Minuten)
2. Oder: Wechsel zu `gpt-4o-mini-transcribe` (schneller) in `config.json`

### Logs einsehen

```powershell
# Aktuelles Log öffnen
notepad "$env:APPDATA\OpenAIDictate\logs\app_$(Get-Date -Format yyyy-MM-dd).log"
```

---

## Support & Updates

### Versionskontrolle

Aktuelle Version im Log:
```
2025-01-15 10:00:00.000 [INFO] OpenAIDictate starting...
2025-01-15 10:00:00.001 [INFO] Version: 1.1.0
```

### Update-Prozess

**Netzlaufwerk-Deployment**:
1. Ersetzen Sie `OpenAIDictate.exe` auf Fileserver
2. Benutzer starten Anwendung neu → automatisch aktualisierte Version

**Lokale Installation**:
1. Führen Sie Deployment-Skript erneut aus
2. Überschreibt alte Version

**Config-Migration**:
- Konfiguration in `%APPDATA%` bleibt bei Updates erhalten

---

**Dokumentversion**: 1.1.0
**Letzte Aktualisierung**: 2025-01-15
