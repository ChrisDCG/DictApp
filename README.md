# OpenAIDictate

**Professional voice dictation for Windows using OpenAI's state-of-the-art gpt-4o-transcribe model**

## Features

- 🎯 **Maximum Transcription Quality**: Uses OpenAI's latest `gpt-4o-transcribe` model (March 2025, SOTA)
- ⚡ **Customizable Hotkey**: Default F5, configurable (Ctrl/Alt/Shift combos) right from the Settings UI
- 🔒 **Secure**: DPAPI encryption for API keys, no admin rights required
- 📦 **Zero Installation**: Single-file EXE, xcopy deployment
- 🎤 **Optimized Audio**: 16kHz, 16-bit PCM mono - optimal for speech recognition
- 🌐 **Offline Detection**: Background probe prevents recordings when the OpenAI API is unreachable
- 🧠 **Smart Processing**:
  - Custom glossaries for domain-specific terminology
  - LLM-based post-processing for punctuation and formatting
  - Audio format validation before upload (rejects non 16kHz/16-bit mono input)
  - Configurable prompting strategies (based on OpenAI Cookbook best practices)
- 💰 **Cost-Effective**: ~$0.006/minute (gpt-4o-transcribe) or ~$0.003/minute (gpt-4o-mini-transcribe)

## Requirements

- **Windows 10/11** (x64)
- **.NET 8 Runtime** (included in self-contained build)
- **OpenAI API Key** (from [platform.openai.com](https://platform.openai.com))
- **Microphone** (any standard USB or built-in mic)

## Quick Start

### 1. Build (Windows only)

```powershell
cd OpenAIDictate
dotnet restore
dotnet build -c Release
```

### 2. Publish Single-File EXE

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\OpenAIDictate.exe`

### 3. Deploy

Copy `OpenAIDictate.exe` to any location (no installer needed):
- Network drive
- `%USERPROFILE%\Tools\`
- USB stick
- etc.

### 4. First Run

1. Double-click `OpenAIDictate.exe`
2. Enter your OpenAI API key when prompted
3. Application runs in system tray
4. Press your configured hotkey (default **F5**) to start dictating!

## Usage

### Basic Workflow

1. Open any application (Outlook, Word, Notepad, etc.)
2. Click where you want to insert text
3. Press your configured hotkey (default **F5**) to start recording
4. Speak your text
5. Press the same hotkey again to stop → transcription appears at cursor position

### Offline Mode Detection

- The tray tooltip switches to `(... Offline)` and a warning balloon pops up when `api.openai.com` is unreachable
- OpenAIDictate blocks new recordings/transcriptions while offline to avoid losing dictations
- Connectivity checks run every 30 seconds and right before uploads

### Configuration

Configuration is stored in: `%APPDATA%\OpenAIDictate\config.json`

```json
{
  "model": "gpt-4o-transcribe",
  "hotkeyGesture": "Ctrl+F5",
  "language": "de",
  "maxRecordingMinutes": 10,
  "glossary": "Bundesgerichtshof, Schadensersatz, §§ 280, 241 BGB",
  "enablePostProcessing": true,
  "enableVAD": true,
  "silenceThresholdDb": -20.0
}
```

#### Key Settings

- **model**: `gpt-4o-transcribe` (best quality) or `gpt-4o-mini-transcribe` (faster, cheaper)
- **hotkeyGesture**: e.g. `F5`, `Ctrl+Shift+F10` – configure the global start/stop hotkey without restarting the app
- **language**: ISO code (`de`, `en`, `fr`, etc.) - improves accuracy
- **glossary**: Domain-specific terms for better recognition
- **enablePostProcessing**: LLM-based punctuation/formatting (GPT-4o-mini)
- **enableVAD**: Voice Activity Detection (removes silence)

### Environment Variables

Override config via environment variables:

```powershell
# API Key (highest priority)
$env:OPENAI_API_KEY = "sk-..."

# Model override
$env:OPENAI_TRANSCRIBE_MODEL = "gpt-4o-mini-transcribe"
```

## Architecture

### Components

```
OpenAIDictate/
├── Models/
│   ├── AppConfig.cs          # Configuration model
│   └── AppState.cs            # State machine states
├── Services/
│   ├── AudioRecorder.cs       # NAudio-based recording (16kHz mono)
│   ├── TranscriptionService.cs # OpenAI API client (gpt-4o-transcribe)
│   ├── GlobalHotkeyService.cs # Global hotkey via WinAPI
│   ├── PromptGenerator.cs     # GPT-generated prompts/context
│   ├── AudioPreprocessor.cs   # Silence trimming & VAD
│   ├── AudioFormatValidator.cs # WAV header validation (16kHz/16-bit mono)
│   ├── TextInjector.cs        # Clipboard + SendInput (Ctrl+V)
│   ├── ConfigService.cs       # JSON config management
│   ├── SecretStore.cs         # DPAPI encryption
│   ├── NetworkStatusService.cs # Offline detection via OpenAI probes
│   ├── OpenAIHttpClientFactory.cs # Shared handler for connection pooling
│   └── Logger.cs              # Technical logging (no audio/text)
├── AppTrayContext.cs          # Main application logic + state machine
└── Program.cs                 # Entry point
```

### State Machine

```
Idle → (Hotkey) → Recording → (Hotkey) → Transcribing → Idle
                  ↑                    ↓
                  └────── (error) ─────┘
```

- **Idle**: Ready to record
- **Recording**: Capturing audio (RAM only, never written to disk)
- **Transcribing**: API call + post-processing + text injection

### Best Practices Implemented

Based on [OpenAI Cookbook](https://cookbook.openai.com):

1. **Audio Quality**: 16kHz, 16-bit PCM, mono (optimal for Whisper/gpt-4o-transcribe)
2. **Prompting Strategy**:
   - Custom glossaries in natural sentences
   - Long prompts (>224 tokens for better steering)
   - Temperature=0 for deterministic output
3. **Post-Processing**: GPT-4o-mini for punctuation/formatting
4. **Error Handling**: Retry logic, exponential backoff
5. **Audio Validation**: Ensures WAV streams are 16kHz/16-bit mono before upload
6. **Connection Pooling**: Shared HTTP handler reuses TLS sessions for faster calls

## Benchmarks

### Accuracy Target

- **Goal**: ≥99% (Dragon Professional level)
- **Model**: gpt-4o-transcribe (March 2025 SOTA)

### Performance

- **Latency**: <100ms from hotkey press to recording start
- **Transcription Time**: ~5s for 60s audio (depends on network)
- **Cost**: $0.006/minute (gpt-4o-transcribe) or $0.003/minute (gpt-4o-mini)

### Cost

- **gpt-4o-transcribe**: $0.006/minute (~$0.36/hour)
- **gpt-4o-mini-transcribe**: $0.003/minute (~$0.18/hour)

Compare to Dragon Professional: €65/month

## Comparison: OpenAIDictate vs Dragon Professional

| Feature | Dragon Pro | OpenAIDictate |
|---------|-----------|---------------|
| **Accuracy** | 99% | ≥99% (gpt-4o) |
| **Cost** | €65/month | ~$0.006/min |
| **Setup** | Complex, lengthy training | Instant, xcopy deploy |
| **Hotkey** | Configurable | Configurable (GUI) |
| **Integration** | Office-specific | Universal (any app) |
| **Offline** | ✅ Yes | ❌ No (API-based) |
| **Admin Rights** | Often required | ❌ Not required |
| **Custom Vocab** | Profile-based | Glossary (instant) |

**Advantage**: Best-in-class cloud transcription, minimal cost, zero installation overhead

## Troubleshooting

### "Failed to register hotkey"

Another application is using your selected gesture. Close conflicting apps or open **Einstellungen → Hotkey** to pick a different combination (e.g., `Ctrl+Shift+F10`).

### "Failed to start recording"

1. Check microphone permissions (Windows Settings → Privacy → Microphone)
2. Verify default recording device is set correctly
3. Check logs: `%APPDATA%\OpenAIDictate\logs\`

### "OpenAI API error 401"

Invalid API key. Check:
1. `OPENAI_API_KEY` environment variable
2. Encrypted key in `%APPDATA%\OpenAIDictate\config.json`

### "Transcription timeout"

Network too slow or recording too long. Try:
1. Shorter recordings
2. Better internet connection
3. Switch to `gpt-4o-mini-transcribe` (faster)

## Logging

Logs are stored in: `%APPDATA%\OpenAIDictate\logs\app_YYYY-MM-DD.log`

**Important**: Only technical metadata is logged (timestamps, durations, HTTP status codes).
**No audio content or transcription text is ever logged.**

## Security

- ✅ **DPAPI Encryption**: API keys encrypted per-user (Windows Data Protection API)
- ✅ **No Disk I/O**: Audio kept in RAM only
- ✅ **HTTPS Only**: All API calls over TLS
- ✅ **No Admin Rights**: Runs in standard user context (`asInvoker`)
- ✅ **No Registry**: Configuration in `%APPDATA%` only
- ✅ **No Telemetry**: No data sent anywhere except OpenAI API

## Roadmap

- [ ] **Silero VAD Integration**: Advanced voice activity detection
- [x] **GPT-Generated Prompts**: Dynamic context generation
- [x] **Settings UI**: GUI for configuration
- [x] **Hotkey Customization**: User-configurable hotkeys
- [ ] **Multi-Language**: UI localization
- [ ] **Code Signing**: Authenticode certificate

## License

MIT License - see LICENSE file

## Credits

- **OpenAI** for the gpt-4o-transcribe model and Cookbook
- **NAudio** for audio recording capabilities
- Built with ❤️ for professionals who value quality and efficiency

---

**Version**: 1.1.0
**Author**: OpenAIDictate Contributors
**Website**: [github.com/yourrepo/OpenAIDictate](https://github.com)
