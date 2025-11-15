# OpenAIDictate API Overview

This document summarizes the most important application services that external contributors interact with when extending OpenAIDictate. All namespaces reside under `OpenAIDictate`.

## Services Layer

### `ConfigService`
Responsible for loading and persisting the user configuration (`%APPDATA%/OpenAIDictate/config.json`).

- `Load()` → returns an `AppConfig` instance, gracefully falling back to defaults when the persisted file is missing or malformed.
- `Save(AppConfig config)` → persists the supplied configuration using indented JSON serialization.
- `GetApiKey(AppConfig config)` → resolves the OpenAI API key from environment variables or the encrypted configuration store.
- `SetApiKey(AppConfig config, string apiKey)` → encrypts and stores the provided API key.
- `GetModel(AppConfig config)` → determines the transcription model with environment-variable override support.

### `ModelAssetManager`
Guarantees availability of the external Silero VAD ONNX model required by the audio pipeline.

- `EnsureSileroVadModelAsync(string? overridePath, CancellationToken cancellationToken = default)`
  - Returns a fully qualified path to a verified ONNX file.
  - Prefers explicit overrides, then environment variables, then cached assets in `%APPDATA%/OpenAIDictate/Models`.
  - Downloads the model from the official Silero repository when necessary and verifies the SHA-256 checksum.

### `SerilogLogger`
Thin wrapper around Serilog that standardizes structured logging for the desktop application.

- Provides `LogInfo`, `LogDebug`, `LogWarning`, and `LogError` helpers.
- Initializes rolling file sinks (plain text + compact JSON) under `%APPDATA%/OpenAIDictate/logs`.
- Implements `IDisposable` to flush log buffers on shutdown.

## Models

### `AppConfig`
POCO describing persisted application preferences such as transcription model, hotkey configuration, localization settings, and Silero VAD parameters. Acts as the single source of truth for the user-facing Settings UI.

## Infrastructure

### `AppTrayContext`
Implements the Windows system tray lifecycle, orchestrating recording, transcription, and UI updates.

### `SettingsForm`
Windows Forms UI that exposes configuration options to end users, including glossary management, hotkey customization, and diagnostics.

## Cross-Cutting Concerns

- **Dependency Injection** is configured in `Program.cs` using `Microsoft.Extensions.DependencyInjection`.
- **Secret Management** is handled by `SecretStore`, which encrypts secrets with Windows DPAPI (`DataProtectionScope.CurrentUser`).

For deeper architectural discussions see `PROJECT_SUMMARY.md`. When adding new public surface area, update this document to keep the API contract discoverable.
