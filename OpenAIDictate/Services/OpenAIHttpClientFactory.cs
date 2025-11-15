using System.Net;
using System.Net.Http;

namespace OpenAIDictate.Services;

/// <summary>
/// Provides pooled <see cref="HttpClient"/> instances configured for the OpenAI API.
/// Sharing a single <see cref="SocketsHttpHandler"/> enables efficient connection reuse.
/// </summary>
internal static class OpenAIHttpClientFactory
{
    private static readonly SocketsHttpHandler SharedHandler = new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        MaxConnectionsPerServer = 20
    };

    /// <summary>
    /// Creates an <see cref="HttpClient"/> that participates in connection pooling.
    /// Callers can specify their desired timeout without sacrificing pooled handlers.
    /// </summary>
    public static HttpClient Create(TimeSpan timeout)
    {
        return new HttpClient(SharedHandler, disposeHandler: false)
        {
            Timeout = timeout
        };
    }
}
