using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAIDictate.Infrastructure;
using OpenAIDictate.Models;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for ServiceCollectionExtensions (DI configuration)
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplicationServices_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddApplicationServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IAppLogger>().Should().NotBeNull();
        serviceProvider.GetService<IMetricsService>().Should().NotBeNull();
        serviceProvider.GetService<NetworkStatusService>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<AppConfig>>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterAppTrayContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddApplicationServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<AppTrayContext>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationServices_ShouldConfigureAppConfig()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddApplicationServices(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var configOptions = serviceProvider.GetRequiredService<IOptions<AppConfig>>();

        // Assert
        configOptions.Value.Should().NotBeNull();
        configOptions.Value.Model.Should().NotBeNullOrEmpty();
    }
}

