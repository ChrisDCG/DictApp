# OpenAIDictate - Quick Start Guide

## Schnellstart in 3 Schritten

### 1. Setup ausf\u00fchren

```powershell
.\setup.ps1
```

Dieses Skript:
- Pr\u00fcft ob .NET SDK installiert ist
- Bietet Download und Installation an
- Verifiziert alle Voraussetzungen
- Bereitet das Projekt vor

**Hinweis:** Falls das .NET SDK fehlt, wird der Installer heruntergeladen. Nach der Installation muss das Setup-Skript erneut ausgef\u00fchrt werden.

---

### 2. Projekt bauen

```powershell
.\build.ps1
```

Optionale Parameter:
```powershell
.\build.ps1 -Configuration Release  # Standard
.\build.ps1 -Configuration Debug     # Debug-Build
.\build.ps1 -Configuration Both      # Beide Konfigurationen
.\build.ps1 -Clean                   # Vorher aufr\u00e4umen
.\build.ps1 -Verbose                 # Ausf\u00fchrliche Ausgabe
```

---

### 3. Anwendung starten

```powershell
.\run.ps1
```

Optionale Parameter:
```powershell
.\run.ps1 -Configuration Release  # Release-Version ausf\u00fchren
.\run.ps1 -Configuration Debug    # Debug-Version ausf\u00fchren
.\run.ps1 -Build                  # Vorher bauen
```

Die Anwendung startet im System Tray (Taskleiste, unten rechts).

---

## Erste Konfiguration

Nach dem ersten Start:

1. **Rechtsklick auf Tray-Icon** \u2192 "Settings"
2. **OpenAI API Key eingeben**
   - Holen Sie Ihren Key von: https://platform.openai.com/api-keys
   - Der Key wird verschl\u00fcsselt gespeichert (Windows DPAPI)
3. **Hotkey konfigurieren** (Standard: F5)
4. **Sprache w\u00e4hlen** (Deutsch, Englisch, Auto)
5. **Model w\u00e4hlen** (gpt-4o-mini empfohlen)
6. **"Save" klicken**

---

## Nutzung

1. **Hotkey dr\u00fccken** (z.B. F5) \u2192 Aufnahme startet
2. **Sprechen**
3. **Hotkey erneut dr\u00fccken** \u2192 Aufnahme stoppt, Transkription beginnt
4. **Text erscheint** automatisch an der Cursor-Position

**Funktioniert in allen Anwendungen:**
- Notepad
- Word
- Outlook
- Browser (Chrome, Edge, Firefox)
- IDEs (Visual Studio Code, etc.)
- Excel, PowerPoint
- und viele mehr

---

## Unit Tests ausf\u00fchren

```powershell
.\test.ps1
```

Optionale Parameter:
```powershell
.\test.ps1 -Configuration Release  # Release-Tests
.\test.ps1 -Configuration Debug    # Debug-Tests
.\test.ps1 -Verbose                # Ausf\u00fchrliche Ausgabe
.\test.ps1 -Coverage               # Mit Code Coverage
```

---

## Deployment-Paket erstellen

```powershell
.\publish.ps1
```

Erstellt ein produktionsreifes Deployment im `publish` Ordner.

Optionale Parameter:
```powershell
.\publish.ps1 -OutputPath "mein-ordner"  # Anderer Ausgabe-Ordner
.\publish.ps1 -CreateZip                 # ZIP-Archiv erstellen
.\publish.ps1 -Verbose                   # Ausf\u00fchrliche Ausgabe
```

Das Deployment enth\u00e4lt:
- **OpenAIDictate.exe** (~50 MB, Self-Contained, Single-File)
- **README.txt** (Benutzer-Anleitung)

Die .exe-Datei kann direkt auf jedem Windows 10/11 System ausgef\u00fchrt werden, ohne Installation!

---

## Probleml\u00f6sung

### Setup-Skript meldet "SDK not found"

**L\u00f6sung:**
1. Download .NET 8.0 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
2. Installer ausf\u00fchren: `dotnet-sdk-8.0.xxx-win-x64.exe`
3. Setup-Skript erneut ausf\u00fchren

**WICHTIG:** "SDK" ausw\u00e4hlen, nicht nur "Runtime"!

---

### Build schl\u00e4gt fehl

**L\u00f6sung:**
```powershell
# NuGet Pakete neu laden
dotnet restore OpenAIDictate\OpenAIDictate.csproj

# Aufr\u00e4umen und neu bauen
.\build.ps1 -Clean -Verbose
```

---

### Anwendung startet nicht

**Pr\u00fcfen:**
1. Ist bereits eine Instanz aktiv? \u2192 `Get-Process OpenAIDictate`
2. Logs pr\u00fcfen: `%APPDATA%\OpenAIDictate\logs\`
3. Als Administrator ausf\u00fchren

---

### Hotkey funktioniert nicht

**L\u00f6sung:**
1. Settings \u00f6ffnen
2. Anderen Hotkey w\u00e4hlen (z.B. Ctrl+Alt+D)
3. Pr\u00fcfen ob anderes Programm den Hotkey verwendet

---

### Mikrofon wird nicht erkannt

**L\u00f6sung:**
1. Windows-Einstellungen \u2192 Datenschutz \u2192 Mikrofon
2. "Desktop-Apps d\u00fcrfen auf Ihr Mikrofon zugreifen" aktivieren
3. Anwendung neu starten

---

### API-Fehler (401 Unauthorized)

**L\u00f6sung:**
1. Settings \u00f6ffnen
2. API Key pr\u00fcfen/neu eingeben
3. Key-G\u00fcltigkeit pr\u00fcfen: https://platform.openai.com/api-keys
4. OpenAI-Konto auf Credits pr\u00fcfen

---

## Weitere Dokumentation

- **[WINDOWS_TEST_GUIDE.md](WINDOWS_TEST_GUIDE.md)** - Detaillierte Test-Anleitung
- **[WINDOWS_COMPATIBILITY_REPORT.md](WINDOWS_COMPATIBILITY_REPORT.md)** - Technischer Bericht
- **Logs:** `%APPDATA%\OpenAIDictate\logs\`
- **Config:** `%APPDATA%\OpenAIDictate\config.json`

---

## Skript-\u00dcbersicht

| Skript | Zweck | Beispiel |
|--------|-------|----------|
| `setup.ps1` | Entwicklungsumgebung einrichten | `.\setup.ps1` |
| `build.ps1` | Projekt bauen | `.\build.ps1 -Clean` |
| `test.ps1` | Unit Tests ausf\u00fchren | `.\test.ps1 -Verbose` |
| `run.ps1` | Anwendung starten | `.\run.ps1 -Build` |
| `publish.ps1` | Deployment erstellen | `.\publish.ps1 -CreateZip` |

---

## Workflow f\u00fcr Entwickler

```powershell
# 1. Einmalig: Setup
.\setup.ps1

# 2. Entwicklungszyklus
.\build.ps1 -Clean        # Bauen
.\test.ps1                # Testen
.\run.ps1                 # Lokal testen

# 3. Deployment
.\publish.ps1 -CreateZip  # Produktions-Paket
```

---

## Workflow f\u00fcr Endnutzer

**Option A: Vorhandenen Build nutzen**
```powershell
# Falls bereits gebaut:
.\run.ps1
```

**Option B: Deployment-Paket**
```powershell
# Deployment erstellen:
.\publish.ps1 -CreateZip

# Dann: ZIP an Endnutzer verteilen
# Endnutzer: Entpacken und OpenAIDictate.exe ausf\u00fchren
```

---

## Systemanforderungen

**Betriebssystem:**
- Windows 10 (Version 1809 oder neuer)
- Windows 11 (alle Versionen)

**Hardware:**
- CPU: x64 (64-bit)
- RAM: Mindestens 2 GB verf\u00fcgbar
- Festplatte: 100 MB freier Speicher
- Mikrofon (beliebig)

**Software:**
- F\u00fcr Entwicklung: .NET 8.0 SDK
- F\u00fcr Endnutzer: Nichts (Self-Contained Deployment)
- Internet-Verbindung (f\u00fcr OpenAI API)
- OpenAI API Key mit Credits

**Berechtigungen:**
- Mikrofon-Zugriff
- Netzwerk-Zugriff
- Clipboard-Zugriff
- Keine Administrator-Rechte erforderlich (au\u00dfer Installation)

---

## Support

Bei Problemen:
1. Logs pr\u00fcfen: `%APPDATA%\OpenAIDictate\logs\`
2. Setup erneut ausf\u00fchren: `.\setup.ps1`
3. Build mit Verbose: `.\build.ps1 -Verbose`
4. Dokumentation lesen (siehe oben)

---

**Viel Erfolg mit OpenAIDictate!** \ud83c\udfa4\u2192\ud83d\udcdd
