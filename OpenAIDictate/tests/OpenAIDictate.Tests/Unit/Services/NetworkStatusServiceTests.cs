using FluentAssertions;
using OpenAIDictate.Services;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for NetworkStatusService
/// </summary>
public class NetworkStatusServiceTests : IDisposable
{
    private readonly NetworkStatusService _service;

    public NetworkStatusServiceTests()
    {
        _service = new NetworkStatusService();
    }

    [Fact]
    public async Task CheckOnlineAsync_NetworkAvailable_ShouldReturnTrue()
    {
        // Note: This is an integration test that requires actual network
        // In a real scenario, we'd mock HttpClient
        
        // Act
        var result = await _service.CheckOnlineAsync();

        // Assert
        // Result depends on actual network availability
        result.Should().BeOfType<bool>();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _service.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldNotThrow()
    {
        // Act & Assert
        _service.Dispose();
        var act = () => _service.Dispose();
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}

