using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Unmeshed.Sdk.Configuration;
using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Poller;

/// <summary>
/// Interface for polling work requests.
/// </summary>
public interface IPollerClient
{
    /// <summary>
    /// Polls for work requests from the Unmeshed engine.
    /// </summary>
    /// <param name="stepSizes">The step queues to poll.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of work requests.</returns>
    Task<List<WorkRequest>> PollAsync(List<StepSize> stepSizes, CancellationToken cancellationToken);
}

/// <summary>
/// Client for polling work requests.
/// </summary>
public class PollerClient : IPollerClient
{
    private readonly HttpClient _httpClient;
    private readonly ClientConfig _config;
    private readonly string _unmeshedHostName;
    private readonly ILogger<PollerClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollerClient"/> class.
    /// </summary>
    public PollerClient(
        Http.IHttpClientFactory httpClientFactory,
        ClientConfig config,
        string unmeshedHostName,
        ILoggerFactory loggerFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
        _unmeshedHostName = unmeshedHostName;
        _logger = loggerFactory.CreateLogger<PollerClient>();
    }

    /// <summary>
    /// Polls for work requests asynchronously.
    /// </summary>
    public async Task<List<WorkRequest>> PollAsync(
        List<StepSize> pollSizes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pollSizes == null || pollSizes.Count == 0)
            {
                return new List<WorkRequest>();
            }

            _logger.LogDebug("Polling for work with {Count} step sizes", pollSizes.Count);

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/clients/poll")
            {
                Content = JsonContent.Create(pollSizes)
            };
            request.Headers.Add("UNMESHED_HOST_NAME", _unmeshedHostName);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var workRequests = await response.Content.ReadFromJsonAsync<List<WorkRequest>>(
                cancellationToken: cancellationToken) ?? new List<WorkRequest>();

            _logger.LogDebug("Received {Count} work requests", workRequests.Count);

            return workRequests;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while polling for work");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Polling request was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while polling for work");
            throw;
        }
    }
}