using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAIDictate;
using OpenAIDictate.Models;
using OpenAIDictate.Services;

namespace OpenAIDictate.Infrastructure;

/// <summary>
/// Dependency injection configuration extensions
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures all application services with dependency injection
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options pattern for AppConfig
        services.Configure<AppConfig>(options =>
        {
            var config = ConfigService.Load();
            LocalizationService.ApplyCulture(config.UiCulture);

            options.Model = config.Model;
            options.HotkeyGesture = config.HotkeyGesture;
            options.HotkeyModifiers = config.HotkeyModifiers;
            options.HotkeyVirtualKey = config.HotkeyVirtualKey;
            options.MaxRecordingMinutes = config.MaxRecordingMinutes;
            options.Language = config.Language;
            options.Glossary = config.Glossary;
            options.EnablePostProcessing = config.EnablePostProcessing;
            options.EnableVAD = config.EnableVAD;
            options.SilenceThresholdDb = config.SilenceThresholdDb;
            options.UiCulture = config.UiCulture;
            options.VadSpeechThreshold = config.VadSpeechThreshold;
            options.VadMinSilenceDurationMs = config.VadMinSilenceDurationMs;
            options.VadMinSpeechDurationMs = config.VadMinSpeechDurationMs;
            options.VadSpeechPaddingMs = config.VadSpeechPaddingMs;
            options.ApiKeyEncrypted = config.ApiKeyEncrypted;
        });

        // Register logging
        services.AddSingleton<IAppLogger, SerilogLogger>();

        // Register metrics
        services.AddSingleton<IMetricsService, MetricsService>();

        // Register services
        services.AddSingleton<NetworkStatusService>();
        services.AddSingleton<ConfigService>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppConfig>>().Value);

        // Register factory-based services (created per request)
        services.AddTransient<AudioRecorder>(sp =>
        {
            var config = sp.GetRequiredService<AppConfig>();
            return new AudioRecorder(config);
        });
        services.AddTransient<TranscriptionService>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<AppConfig>>().Value;
            var logger = sp.GetRequiredService<IAppLogger>();
            var metrics = sp.GetRequiredService<IMetricsService>();
            var preprocessor = sp.GetRequiredService<AudioPreprocessor>();
            var apiKey = ConfigService.GetApiKey(config);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("API key not configured");
            }

            return new TranscriptionService(config, apiKey, logger, metrics, preprocessor);
        });

        services.AddTransient<AudioPreprocessor>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<AppConfig>>().Value;
            return new AudioPreprocessor(config);
        });

        // Register main application context with DI support
        services.AddSingleton<AppTrayContext>(sp =>
        {
            var configOptions = sp.GetRequiredService<IOptions<AppConfig>>();
            var logger = sp.GetRequiredService<IAppLogger>();
            var metrics = sp.GetRequiredService<IMetricsService>();
            var networkService = sp.GetRequiredService<NetworkStatusService>();
            
            return new AppTrayContext(configOptions, logger, metrics, networkService, sp);
        });

        return services;
    }
}

