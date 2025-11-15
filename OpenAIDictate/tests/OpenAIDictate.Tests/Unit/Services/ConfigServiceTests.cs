using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using OpenAIDictate.Models;
using OpenAIDictate.Services;
using System.Text.Json;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for ConfigService
/// </summary>
public class ConfigServiceTests : IDisposable
{
    private readonly string _testConfigDirectory;
    private readonly string _testConfigFilePath;

    public ConfigServiceTests()
    {
        _testConfigDirectory = Path.Combine(Path.GetTempPath(), "OpenAIDictateTests", Guid.NewGuid().ToString());
        _testConfigFilePath = Path.Combine(_testConfigDirectory, "config.json");
        Directory.CreateDirectory(_testConfigDirectory);
    }

    [Fact]
    public void Load_NonExistentConfig_ShouldReturnDefaultConfig()
    {
        // Act
        var config = ConfigService.Load();

        // Assert
        config.Should().NotBeNull();
        config.Model.Should().Be("gpt-4o-transcribe");
        config.HotkeyGesture.Should().Be("F5");
    }

    [Fact]
    public void Save_ValidConfig_ShouldCreateConfigFile()
    {
        // Arrange
        var config = new AppConfig
        {
            Model = "gpt-4o-mini-transcribe",
            HotkeyGesture = "Ctrl+F5",
            Language = "en",
            MaxRecordingMinutes = 15
        };

        // Act
        ConfigService.Save(config);

        // Assert
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenAIDictate",
            "config.json"
        );
        File.Exists(configPath).Should().BeTrue();
    }

    [Fact]
    public void Load_SavedConfig_ShouldReturnSavedValues()
    {
        // Arrange
        var originalConfig = new AppConfig
        {
            Model = "gpt-4o-mini-transcribe",
            HotkeyGesture = "Ctrl+F5",
            Language = "en",
            MaxRecordingMinutes = 15,
            EnablePostProcessing = false
        };
        ConfigService.Save(originalConfig);

        // Act
        var loadedConfig = ConfigService.Load();

        // Assert
        loadedConfig.Model.Should().Be(originalConfig.Model);
        loadedConfig.HotkeyGesture.Should().Be(originalConfig.HotkeyGesture);
        loadedConfig.Language.Should().Be(originalConfig.Language);
        loadedConfig.MaxRecordingMinutes.Should().Be(originalConfig.MaxRecordingMinutes);
        loadedConfig.EnablePostProcessing.Should().Be(originalConfig.EnablePostProcessing);
    }

    [Fact]
    public void GetApiKey_EnvironmentVariableSet_ShouldReturnEnvValue()
    {
        // Arrange
        const string testKey = "sk-test-env-key";
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", testKey);

        try
        {
            var config = new AppConfig();

            // Act
            var apiKey = ConfigService.GetApiKey(config);

            // Assert
            apiKey.Should().Be(testKey);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public void GetApiKey_EncryptedConfigSet_ShouldDecryptAndReturn()
    {
        // Arrange
        const string testKey = "sk-test-config-key";
        var config = new AppConfig
        {
            ApiKeyEncrypted = SecretStore.Encrypt(testKey)
        };

        // Act
        var apiKey = ConfigService.GetApiKey(config);

        // Assert
        apiKey.Should().Be(testKey);
    }

    [Fact]
    public void SetApiKey_ValidKey_ShouldEncryptAndSave()
    {
        // Arrange
        const string testKey = "sk-test-set-key";
        var config = new AppConfig();

        // Act
        ConfigService.SetApiKey(config, testKey);

        // Assert
        config.ApiKeyEncrypted.Should().NotBeNullOrEmpty();
        config.ApiKeyEncrypted.Should().NotBe(testKey);
        
        var decrypted = SecretStore.Decrypt(config.ApiKeyEncrypted);
        decrypted.Should().Be(testKey);
    }

    [Fact]
    public void SetApiKey_EmptyKey_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new AppConfig();

        // Act & Assert
        var act = () => ConfigService.SetApiKey(config, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetModel_EnvironmentVariableSet_ShouldReturnEnvValue()
    {
        // Arrange
        const string testModel = "gpt-4o-mini-transcribe";
        Environment.SetEnvironmentVariable("OPENAI_TRANSCRIBE_MODEL", testModel);

        try
        {
            var config = new AppConfig { Model = "gpt-4o-transcribe" };

            // Act
            var model = ConfigService.GetModel(config);

            // Assert
            model.Should().Be(testModel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_TRANSCRIBE_MODEL", null);
        }
    }

    [Fact]
    public void GetModel_NoEnvironmentVariable_ShouldReturnConfigValue()
    {
        // Arrange
        var config = new AppConfig { Model = "gpt-4o-transcribe" };

        // Act
        var model = ConfigService.GetModel(config);

        // Assert
        model.Should().Be("gpt-4o-transcribe");
    }

    public void Dispose()
    {
        // Cleanup test files
        try
        {
            if (Directory.Exists(_testConfigDirectory))
            {
                Directory.Delete(_testConfigDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

