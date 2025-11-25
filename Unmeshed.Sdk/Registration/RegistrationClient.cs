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
    /// </summary>
    public async Task RenewRegistrationAsync(CancellationToken cancellationToken = default)
    {
        if (_workers.Count == 0)
        {
            _logger.LogWarning("No workers to register");
            return;
        }

        try
        {
            // Convert workers to StepQueueName format (matching Java SDK)
            var stepQueueNames = _workers.Select(w => new
            {
                processId = 0,
                @namespace = w.Namespace,
                stepType = UnmeshedConstants.StepType.Worker,
                name = w.Name
            }).ToList();

            _logger.LogInformation("Registering {Count} workers with engine", _workers.Count);

            // Use PUT request (not POST) to match Java SDK
            var request = new HttpRequestMessage(HttpMethod.Put, "api/clients/register")
            {
                Content = JsonContent.Create(stepQueueNames)
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully registered workers");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to register workers");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during worker registration");
            throw;
        }
    }
}
