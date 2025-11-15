using System.Text.Json;
using OpenAIDictate.Models;

namespace OpenAIDictate.Services;

/// <summary>
/// Manages application configuration stored in %APPDATA%\OpenAIDictate\config.json
/// </summary>
public class ConfigService
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenAIDictate"
    );

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Loads configuration from disk, or returns default if not found
    /// </summary>
    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return new AppConfig();
            }

            string json = File.ReadAllText(ConfigFilePath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch (Exception ex)
        {
            // Log error but don't crash - return default config
            Logger.LogError($"Failed to load config from {ConfigFilePath}: {ex.Message}");
            return new AppConfig();
        }
    }

    /// <summary>
    /// Saves configuration to disk
    /// </summary>
    public static void Save(AppConfig config)
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(ConfigDirectory);

            string json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save config to {ConfigFilePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the OpenAI API key from environment variable or encrypted config
    /// Priority: OPENAI_API_KEY env var > config.json (DPAPI-encrypted)
    /// </summary>
    public static string? GetApiKey(AppConfig config)
    {
        // 1. Check environment variable first
        string? envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            return envKey;
        }

        // 2. Check encrypted config
        if (!string.IsNullOrWhiteSpace(config.ApiKeyEncrypted))
        {
            try
            {
                return SecretStore.Decrypt(config.ApiKeyEncrypted);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to decrypt API key: {ex.Message}");
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Stores an API key in the configuration (DPAPI-encrypted)
    /// </summary>
    public static void SetApiKey(AppConfig config, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

        config.ApiKeyEncrypted = SecretStore.Encrypt(apiKey);
        Save(config);
    }

    /// <summary>
    /// Gets the transcription model from environment variable or config
    /// Priority: OPENAI_TRANSCRIBE_MODEL env var > config.json
    /// </summary>
    public static string GetModel(AppConfig config)
    {
        string? envModel = Environment.GetEnvironmentVariable("OPENAI_TRANSCRIBE_MODEL");
        return !string.IsNullOrWhiteSpace(envModel) ? envModel : config.Model;
    }
}
