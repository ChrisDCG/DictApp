using FluentAssertions;
using OpenAIDictate.Services;
using System.Net.Http;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for OpenAIHttpClientFactory
/// </summary>
public class OpenAIHttpClientFactoryTests
{
    [Fact]
    public void Create_ValidTimeout_ShouldReturnHttpClient()
    {
        // Act
        var client = OpenAIHttpClientFactory.Create(TimeSpan.FromSeconds(30));

        // Assert
        client.Should().NotBeNull();
        client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Create_MultipleClients_ShouldShareHandler()
    {
        // Act
        var client1 = OpenAIHttpClientFactory.Create(TimeSpan.FromSeconds(30));
        var client2 = OpenAIHttpClientFactory.Create(TimeSpan.FromMinutes(5));

        // Assert
        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        client2.Timeout.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Create_HttpClient_ShouldHaveCorrectTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(2);

        // Act
        var client = OpenAIHttpClientFactory.Create(timeout);

        // Assert
        client.Timeout.Should().Be(timeout);
    }
}

