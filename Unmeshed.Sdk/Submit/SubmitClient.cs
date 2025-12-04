using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Unmeshed.Sdk.Configuration;
using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Submit;

/// <summary>
/// Interface for submitting work responses.
/// </summary>
public interface ISubmitClient
{
    /// <summary>
    /// Submits a work response asynchronously.
    /// </summary>
    Task SubmitAsync(WorkResponse response, SemaphoreSlim semaphore, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets the current size of the submit tracker queue.
    /// </summary>
    int GetSubmitTrackerSize();
}

/// <summary>
/// Implementation of the submit client with async batch processing.
/// </summary>
public class SubmitClient : ISubmitClient
{
    private readonly HttpClient _httpClient;
    private readonly ClientConfig _config;
    private readonly ILogger<SubmitClient> _logger;
    private readonly ConcurrentQueue<WorkResponseTracker> _submitQueue;
    private readonly SemaphoreSlim _batchSemaphore;
    private readonly CancellationTokenSource _shutdownCts;
    private readonly Task _batchProcessorTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitClient"/> class.
    /// </summary>
    public SubmitClient(
        Http.IHttpClientFactory httpClientFactory,
        ClientConfig config,
        ILoggerFactory loggerFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
        _logger = loggerFactory.CreateLogger<SubmitClient>();
        _submitQueue = new ConcurrentQueue<WorkResponseTracker>();
        _batchSemaphore = new SemaphoreSlim(1, 1);
        _shutdownCts = new CancellationTokenSource();

        if (!_config.EnableResultsSubmission)
        {
            _logger.LogWarning("Batch processing is disabled for results submission");
            _batchProcessorTask = Task.CompletedTask;
            return;
        }

        // Start background batch processor
        _batchProcessorTask = Task.Run(() => BatchProcessorAsync(_shutdownCts.Token));
        _logger.LogInformation("Batch processing enabled for submit operations");
    }

    /// <summary>
    /// Submits a work response asynchronously.
    /// </summary>
    public async Task SubmitAsync(
        WorkResponse response,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken = default)
    {

        // Enqueue for batch processing
        var tracker = new WorkResponseTracker
        {
            Response = response,
            Semaphore = semaphore,
            AttemptCount = 0,
            EnqueuedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        _submitQueue.Enqueue(tracker);
        _logger.LogDebug("Enqueued work response for step {StepId}", response.StepId);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current size of the submit tracker queue.
    /// </summary>
    public int GetSubmitTrackerSize()
    {
        return _submitQueue.Count;
    }

    /// <summary>
    /// Background task that processes submissions in batches.
    /// </summary>
    private async Task BatchProcessorAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(100, cancellationToken); // Small delay between batch checks

                if (_submitQueue.IsEmpty)
                {
                    continue;
                }

                await _batchSemaphore.WaitAsync(cancellationToken);
                try
                {
                    var batch = new List<WorkResponseTracker>();
                    var batchSize = Math.Min(_config.ResponseSubmitBatchSize, _submitQueue.Count);

                    for (int i = 0; i < batchSize && _submitQueue.TryDequeue(out var tracker); i++)
                    {
                        batch.Add(tracker);
                    }

                    if (batch.Count > 0)
                    {
                        await ProcessBatchAsync(batch, cancellationToken);
                    }
                }
                finally
                {
                    _batchSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch processor");
            }
        }
    }

    /// <summary>
    /// Processes a batch of work responses.
    /// </summary>
    private async Task ProcessBatchAsync(
        List<WorkResponseTracker> batch,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing batch of {Count} work responses", batch.Count);

        var responses = batch.Select(t => t.Response).ToList();

        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync(
                "api/clients/bulkResults",
                responses,
                cancellationToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully submitted batch of {Count} responses", batch.Count);

                // Release semaphores for successful submissions
                foreach (var tracker in batch)
                {
                    tracker.Semaphore?.Release();
                }
            }
            else
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Failed to submit batch. Status: {StatusCode}, Error: {Error}",
                    httpResponse.StatusCode,
                    errorContent);

                // Check if error is permanent
                bool isPermanentError = _config.PermanentErrorKeywords
                    .Any(keyword => errorContent.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                // Retry or release based on error type
                await HandleFailedBatchAsync(batch, isPermanentError, cancellationToken);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while submitting batch");
            await HandleFailedBatchAsync(batch, false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while submitting batch");
            await HandleFailedBatchAsync(batch, false, cancellationToken);
        }
    }

    /// <summary>
    /// Handles failed batch submissions with retry logic.
    /// </summary>
    private async Task HandleFailedBatchAsync(
        List<WorkResponseTracker> batch,
        bool isPermanentError,
        CancellationToken cancellationToken)
    {
        foreach (var tracker in batch)
        {
            tracker.AttemptCount++;

            if (isPermanentError || tracker.AttemptCount >= _config.MaxSubmitAttempts)
            {
                _logger.LogError(
                    "Permanently failed to submit response for step {StepId} after {Attempts} attempts",
                    tracker.Response.StepId,
                    tracker.AttemptCount);

                // Release semaphore even on permanent failure
                tracker.Semaphore?.Release();
            }
            else
            {
                _logger.LogWarning(
                    "Retrying submission for step {StepId}, attempt {Attempt}/{Max}",
                    tracker.Response.StepId,
                    tracker.AttemptCount,
                    _config.MaxSubmitAttempts);

                // Re-enqueue for retry
                _submitQueue.Enqueue(tracker);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the client and stops the background processor.
    /// </summary>
    public void Dispose()
    {
        _shutdownCts.Cancel();
        _batchProcessorTask.Wait(TimeSpan.FromSeconds(5));
        _shutdownCts.Dispose();
        _batchSemaphore.Dispose();
    }
}
