using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAIDictate.Services;

/// <summary>
/// Lightweight connectivity checker that probes the OpenAI API endpoint and reports online/offline status.
/// </summary>
public sealed class NetworkStatusService : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public NetworkStatusService()
    {
        _httpClient = OpenAIHttpClientFactory.Create(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Returns true when we can reach the OpenAI API endpoint (network and DNS available).
    /// </summary>
    public async Task<bool> CheckOnlineAsync(CancellationToken cancellationToken = default)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://api.openai.com/v1");
            request.Headers.UserAgent.ParseAdd("OpenAIDictate/1.1.0");
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            // Treat typical auth and missing-endpoint codes as "online" (API reachable, even if creds missing)
            return response.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.MethodNotAllowed;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _httpClient.Dispose();
        _disposed = true;
    }
}
