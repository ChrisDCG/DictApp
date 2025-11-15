# Changelog

All notable changes to OpenAIDictate will be documented in this file.

## [Unreleased]

### 🚀 Enhancements

- ✅ Added support for OpenAI's `chunking_strategy="auto"` to leverage server-side VAD and loudness normalization for long recordings.
- ✅ Integrated optional diarized transcription via `gpt-4o-transcribe-diarize`, including speaker-segment metrics and UI toggles.
- ✅ Surface token-level log probabilities for QA/regression workflows (gpt-4o/gpt-4o-mini) with gauges in the metrics service.
- ✅ Extended Settings UI with controls for server chunking, log probabilities, and diarized output plus updated model picker.

### 📚 Documentation

- ✅ README updated with chunking/diarization/log-probability guidance and refreshed configuration snippets.

## [1.2.0] - 2025-11-15

### 🚀 Major Features

- ✅ **Silero VAD Integration**: Production-grade ONNX pipeline trims silence/noise with official Silero thresholds.
- ✅ **Multi-Language UI**: Runtime-switchable English/German UI, localized tray menu, and translated dialogs.
- ✅ **Authenticode Pipeline**: `build.ps1` can sign releases automatically via environment-provided certificates.

### 🔧 Improvements

- ✅ Added fine-grained VAD controls (threshold, silence duration, padding) to the Settings UI.
- ✅ Normalized preprocessed audio to avoid clipping after trimming.
- ✅ Settings dialog now surfaces UI language selection with immediate tray updates.
- ✅ Tray notifications and balloon tips fully respect the active culture.
- ✅ Silero VAD model is fetched securely at runtime with checksum validation, eliminating bundled binaries.

### 📚 Documentation

- ✅ README updated with Silero VAD, localization, and code-signing guidance.
- ✅ CHANGELOG updated (this file).

## [1.1.0] - 2025-01-15

### 🚀 Major Features

#### GPT-Generated Prompting Strategy
- ✅ **PromptGenerator Service**: Automatically generates optimized prompts using GPT-4o-mini
- ✅ **Contextual Examples**: Creates realistic, domain-specific example text for better transcription steering
- ✅ **Smart Caching**: Caches generated prompts to avoid redundant API calls
- ✅ **Fallback Support**: Gracefully falls back to basic prompts if GPT generation fails
- ✅ **Natural Glossaries**: Converts term lists into natural sentences (OpenAI Cookbook best practice)

#### Settings UI Dialog
- ✅ **Full GUI Configuration**: No more manual config.json editing
- ✅ **Three Tabs**:
  - **General**: Model selection, language, max recording, glossary
  - **Advanced**: Post-processing, VAD, silence threshold
  - **About**: Version info, features, credits
- ✅ **Real-Time Updates**: Settings applied immediately after save
- ✅ **Service Reinitialization**: Automatically reinitializes TranscriptionService with new config
- ✅ **Input Validation**: Validates all settings before saving

#### Real-Time Recording Duration Display
- ✅ **Live Tray Updates**: Shows recording duration in MM:SS format
- ✅ **Updates Every Second**: Smooth, real-time feedback
- ✅ **Visual Indicator**: Red dot (●) shows recording is active
- ✅ **Non-Blocking**: Runs on separate timer thread

#### Global Hotkey Customization
- ✅ **UI-Driven Configuration**: Hotkeys can be edited directly in the Settings UI (with validation)
- ✅ **Suggested Combos**: Dropdown offers curated key combinations for quick selection
- ✅ **Live Re-Registration**: Hotkey service automatically re-registers without restarting the app
- ✅ **Per-User Storage**: Gesture is persisted in `%APPDATA%` for roaming scenarios

### 🔧 Improvements

- ✅ **Enhanced TranscriptionService**: Now uses PromptGenerator for optimized prompts
- ✅ **Better Error Handling**: Settings dialog gracefully handles errors
- ✅ **Improved Logging**: More detailed logs for prompt generation and settings changes
- ✅ **Code Organization**: New PromptGenerator and SettingsForm classes
- ✅ **Audio Format Validation**: Ensures WAV headers are 16kHz/16-bit mono before hitting the API
- ✅ **HTTP Connection Pooling**: Shared `SocketsHttpHandler` speeds up repeated OpenAI calls
- ✅ **Offline Mode Detection**: Background connectivity checks warn users before starting a dictation

### 📚 Documentation

- ✅ Updated README with v1.1 features
- ✅ Updated CHANGELOG (this file)
- ✅ Updated PROJECT_SUMMARY with new architecture

### 🐛 Bug Fixes

- ✅ Fixed Program.cs syntax (moved STA attributes outside try-catch)
- ✅ Improved timer cleanup in Dispose pattern

---

## [1.0.0] - 2025-01-15

### Initial Release - International Top-Level Quality

#### Core Features
- ✅ **State-of-the-Art Transcription**: OpenAI `gpt-4o-transcribe` model (March 2025 SOTA)
- ✅ **F5 Hotkey**: Universal start/stop recording (works in Outlook, Word, Notepad, etc.)
- ✅ **Zero Installation**: Single-file EXE, xcopy deployment, no admin rights required
- ✅ **Optimal Audio Quality**: 16kHz, 16-bit PCM mono (best for speech recognition)
- ✅ **Secure Storage**: DPAPI encryption for API keys
- ✅ **RAM-Only Recording**: No audio written to disk
- ✅ **Smart Text Injection**: Clipboard + Ctrl+V simulation for universal compatibility

#### Best Practices Implementation (OpenAI Cookbook)
- ✅ **Prompting Strategy**:
  - Custom glossaries for domain-specific terminology
  - Natural sentence examples for better steering
  - Temperature=0 for deterministic output
- ✅ **Post-Processing**: GPT-4o-mini for punctuation and formatting
- ✅ **Error Handling**: Retry logic with exponential backoff
- ✅ **Audio Preprocessing**: Silence detection and trimming

#### Architecture
- ✅ **State Machine**: Idle → Recording → Transcribing → Idle
- ✅ **Tray Application**: Runs in system tray with context menu
- ✅ **Logging**: Technical metadata only (no audio/text content)
- ✅ **Configuration**: JSON-based with environment variable overrides

#### Services
- ✅ `AudioRecorder`: NAudio-based recording (16kHz mono, RAM-only)
- ✅ `TranscriptionService`: OpenAI API client with best practices
- ✅ `GlobalHotkeyService`: F5 hotkey via Windows API
- ✅ `TextInjector`: Clipboard + SendInput (Ctrl+V)
- ✅ `ConfigService`: JSON config management (%APPDATA%)
- ✅ `SecretStore`: DPAPI encryption
- ✅ `Logger`: Technical logging only

#### Configuration Options
- Model selection: `gpt-4o-transcribe`, `gpt-4o-mini-transcribe`, `whisper-1`
- Language specification (improves accuracy)
- Custom glossaries (legal terms, product names, etc.)
- Post-processing toggle
- VAD toggle (Voice Activity Detection)
- Silence threshold customization
- Maximum recording duration

#### Performance Benchmarks
- **Target Accuracy**: ≥99% (Dragon Professional level)
- **Latency**: <100ms from F5 press to recording start
- **Transcription Time**: <5s for 30-60s audio (typical network)
- **Cost**: $0.006/minute (gpt-4o-transcribe) or $0.003/minute (gpt-4o-mini)

#### Security & Privacy
- ✅ DPAPI encryption (user-scoped)
- ✅ No disk I/O for audio
- ✅ HTTPS-only API calls
- ✅ No admin rights required
- ✅ No registry modifications
- ✅ No telemetry

#### Compatibility
- Windows 10/11 x64
- .NET 8.0-windows runtime
- Works with: Outlook, Word, Excel, Notepad, Visual Studio Code, Teams, etc.

### Known Limitations
- ⚠️ Requires internet connection (cloud API)
- ⚠️ F5 hotkey not configurable yet (planned for v1.1)
- ⚠️ No GUI settings dialog yet (manual config.json editing)
- ⚠️ Silero VAD integration pending (planned for v1.1)

---

## [Upcoming - v1.2]

### Planned Features
- [ ] **Silero VAD Integration**: Advanced voice activity detection (1.8MB library)
- [ ] **Hotkey Customization**: User-configurable hotkeys via Settings UI
- [ ] **Multi-Language UI**: German/English/French localization
- [ ] **Audio Format Validation**: Validate recording quality before transcription
- [ ] **Connection Pooling**: Reuse HttpClient connections for better performance
- [ ] **Offline Mode Detection**: Detect and warn about network issues before recording
- [ ] **Code Signing**: Authenticode certificate for SmartScreen/AppLocker

### Optimizations
- [ ] Reduce EXE size through IL trimming (~30% reduction)
- [ ] Add audio preprocessing pipeline (silence trimming, normalization)
- [ ] Implement streaming transcription for very long recordings
- [ ] Add retry strategies for network resilience

---

**Note**: This changelog follows [Keep a Changelog](https://keepachangelog.com/) principles.
