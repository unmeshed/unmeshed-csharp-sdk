using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Unmeshed.Sdk.Workers;
using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Registration;

/// <summary>
/// Interface for worker registration.
/// </summary>
public interface IRegistrationClient
{
    /// <summary>
    /// Renews the registration with the engine.
    /// </summary>
    Task RenewRegistrationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds workers to the registration list.
    /// </summary>
    void AddWorkers(List<Worker> workers);
    
    /// <summary>
    /// Gets the list of registered workers.
    /// </summary>
    List<Worker> GetWorkers();
}

/// <summary>
/// Handles worker registration with the Unmeshed engine.
/// </summary>
public class RegistrationClient : IRegistrationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RegistrationClient> _logger;
    private readonly List<Worker> _workers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrationClient"/> class.
    /// </summary>
    public RegistrationClient(
        Http.IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = loggerFactory.CreateLogger<RegistrationClient>();
        _workers = new List<Worker>();
    }

    /// <summary>
    /// Adds workers to the registration list.
    /// </summary>
    public void AddWorkers(List<Worker> workers)
    {
        _workers.AddRange(workers);
        _logger.LogInformation("Added {Count} workers to registration", workers.Count);
    }

    /// <summary>
    /// Gets the list of registered workers.
    /// </summary>
    public List<Worker> GetWorkers()
    {
        return _workers;
    }

    /// <summary>
    /// Renews worker registration with the engine asynchronously.
    /// Implements retry logic with exponential backoff (1-10 seconds).
    /// </summary>
    public async Task RenewRegistrationAsync(CancellationToken cancellationToken = default)
    {
        if (_workers.Count == 0)
        {
            _logger.LogWarning("No workers to register");
            return;
        }

        // Convert workers to StepQueueName format (matching Java SDK)
        var stepQueueNames = _workers.Select(w => new
        {
            processId = 0,
            @namespace = w.Namespace,
            stepType = UnmeshedConstants.StepType.Worker,
            name = w.Name
        }).ToList();

        var delay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(10);
        var retryCount = 0;

        while (true)
        {
            try
            {
                _logger.LogInformation("Attempting to renew registration. Retry count: {RetryCount}", retryCount);

                var request = new HttpRequestMessage(HttpMethod.Put, "api/clients/register")
                {
                    Content = JsonContent.Create(stepQueueNames)
                };

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    retryCount = 0;
                    _logger.LogInformation("Successfully renewed registration for workers");
                    return;
                }

                // Handle non-success status codes
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrEmpty(errorBody))
                {
                    _logger.LogWarning("Did not receive 200! Status: {StatusCode}, Error: {ErrorBody}", 
                        response.StatusCode, errorBody);
                }
                else
                {
                    _logger.LogWarning("Did not receive 200! Status: {StatusCode}", response.StatusCode);
                }

                retryCount++;
                _logger.LogInformation("Retry {RetryCount} failed: HTTPError:status code {StatusCode}", 
                    retryCount, response.StatusCode);
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogInformation("Retry {RetryCount} failed: {ExceptionType}:{ExceptionMessage}", 
                    retryCount, ex.GetType().Name, ex.Message);
            }

            _logger.LogInformation("Waiting for {DelaySeconds} seconds before retrying...", 
                (int)delay.TotalSeconds);
            await Task.Delay(delay, cancellationToken);

            // Increment delay, capping at maxDelay
            if (delay < maxDelay)
            {
                delay = delay.Add(TimeSpan.FromSeconds(2));
                if (delay > maxDelay)
                {
                    delay = maxDelay;
                }
            }
        }
    }
}
