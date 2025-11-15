using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using OpenAIDictate.Infrastructure;
using OpenAIDictate.Models;
using OpenAIDictate.Services;

namespace OpenAIDictate.Tests.TestHelpers;

/// <summary>
/// Test fixture for dependency injection setup in tests
/// </summary>
public class TestFixture
{
    public IServiceProvider ServiceProvider { get; }
    public Mock<IAppLogger> MockLogger { get; }
    public Mock<IMetricsService> MockMetrics { get; }
    public AppConfig TestConfig { get; }

    public TestFixture()
    {
        MockLogger = new Mock<IAppLogger>();
        MockMetrics = new Mock<IMetricsService>();
        
        TestConfig = new AppConfig
        {
            Model = "gpt-4o-transcribe",
            HotkeyGesture = "F5",
            Language = "de",
            MaxRecordingMinutes = 10,
            EnablePostProcessing = true,
            EnableVAD = true,
            SilenceThresholdDb = -20.0
        };

        var services = new ServiceCollection();
        
        // Register mocks
        services.AddSingleton(MockLogger.Object);
        services.AddSingleton(MockMetrics.Object);
        
        // Register test configuration
        services.Configure<AppConfig>(options =>
        {
            options.Model = TestConfig.Model;
            options.HotkeyGesture = TestConfig.HotkeyGesture;
            options.Language = TestConfig.Language;
            options.MaxRecordingMinutes = TestConfig.MaxRecordingMinutes;
            options.EnablePostProcessing = TestConfig.EnablePostProcessing;
            options.EnableVAD = TestConfig.EnablePostProcessing;
            options.SilenceThresholdDb = TestConfig.SilenceThresholdDb;
        });

        // Register real services for testing
        services.AddSingleton<NetworkStatusService>();
        services.AddSingleton<ConfigService>();
        services.AddSingleton<SecretStore>();

        ServiceProvider = services.BuildServiceProvider();
    }
}

