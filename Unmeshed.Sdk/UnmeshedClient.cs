using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Unmeshed.Sdk.Configuration;
using Unmeshed.Sdk.Http;
using Unmeshed.Sdk.Models;
using Unmeshed.Sdk.Poller;
using Unmeshed.Sdk.Process;
using Unmeshed.Sdk.Registration;
using Unmeshed.Sdk.Schedulers;
using Unmeshed.Sdk.Submit;
using Unmeshed.Sdk.Workers;

namespace Unmeshed.Sdk;

/// <summary>
/// Main client for interacting with the Unmeshed workflow engine.
/// Provides async worker capabilities with polling and submission.
/// </summary>
public class UnmeshedClient : IDisposable
{
    private readonly ClientConfig _config;
    private readonly ILogger<UnmeshedClient> _logger;
    private readonly ILoggerFactory _loggerFactory;
    
    private readonly IRegistrationClient _registrationClient;
    private readonly IPollerClient _pollerClient;
    private readonly IWorkScheduler _workScheduler;
    private readonly ISubmitClient _submitClient;
    private readonly IProcessClient _processClient;
    private readonly WorkResponseBuilder _responseBuilder;

    private readonly ConcurrentDictionary<string, StepQueuePollState> _pollStates;
    private readonly CancellationTokenSource _pollingCts;
    private Task? _pollingTask;
    private bool _isStarted;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the UnmeshedClient.
    /// </summary>
    /// <param name="config">Client configuration.</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    public UnmeshedClient(ClientConfig config, ILoggerFactory loggerFactory)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<UnmeshedClient>();

        // Validate configuration
        _config.Validate();

        // Initialize HTTP factory
        var httpClientFactory = new HttpClientFactory(_config, _loggerFactory);

        // Initialize clients
        _registrationClient = new RegistrationClient(httpClientFactory, _loggerFactory);
        _pollerClient = new PollerClient(httpClientFactory, _config, GetHostName(), _loggerFactory);
        _workScheduler = new WorkScheduler(_config, _loggerFactory);
        _submitClient = new SubmitClient(httpClientFactory, _config, _loggerFactory);
        _processClient = new ProcessClient(httpClientFactory, _config, _loggerFactory);
        _responseBuilder = new WorkResponseBuilder();

        _pollStates = new ConcurrentDictionary<string, StepQueuePollState>();
        _pollingCts = new CancellationTokenSource();

         var baseUrl = _config.BaseUrl?.TrimEnd('/') ?? "UNKNOWN";

         string endpoint;

         if (baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
         {
             // If already contains port, DO NOT add again
             endpoint = Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                 ? uri.IsDefaultPort ? $"{baseUrl}:{_config.Port}" : baseUrl
                 : baseUrl;
         }
         else
         {
             endpoint = baseUrl;
         }

         _logger.LogInformation("UnmeshedClient initialized for {Endpoint}", endpoint);
    }

    #region Worker Registration

    /// <summary>
    /// Gets the hostname for this client instance.
    /// Checks environment variables in order: UNMESHED_HOST_NAME, HOSTNAME, COMPUTERNAME
    /// Falls back to System.Net.Dns.GetHostName() or "-" if all else fails.
    /// </summary>
    /// <returns>The hostname as a string.</returns>
    public static string GetHostName()
    {
       var unmeshedHostName = Environment.GetEnvironmentVariables()["UNMESHED_HOST_NAME"] as string;
       if (!string.IsNullOrWhiteSpace(unmeshedHostName))
       {
          return unmeshedHostName.Trim();
       }
       // Check HOSTNAME environment variable (Linux, macOS)
       var hostname = Environment.GetEnvironmentVariables()["HOSTNAME"] as string;
       if (!string.IsNullOrWhiteSpace(hostname))
       {
          return hostname.Trim();
       }
       // Check COMPUTERNAME environment variable (Windows)
       hostname = Environment.GetEnvironmentVariables()["COMPUTERNAME"] as string;
       if (!string.IsNullOrWhiteSpace(hostname))
       {
           return hostname.Trim();
       }
       // Try to get hostname from System.Net.Dns
       try
       {
          hostname = System.Net.Dns.GetHostName();
          if (!string.IsNullOrWhiteSpace(hostname))
          {
             return hostname.Trim();
          }
       }
       catch (Exception)
       {
           // Ignore exceptions and fall through to default
       }
       return "-";
    }

    /// <summary>
    /// Registers workers by scanning the specified namespace.
    /// </summary>
    /// <param name="namespacePath">Namespace path to scan for workers.</param>
    public async Task RegisterWorkersAsync(string namespacePath)
    {
        _logger.LogInformation("Scanning for workers in namespace: {Namespace}", namespacePath);

        var workers = WorkerScanner.FindWorkers(namespacePath);
        
        if (workers.Count == 0)
        {
            _logger.LogWarning("No workers found in namespace: {Namespace}", namespacePath);
            return;
        }

        _registrationClient.AddWorkers(workers);

        foreach (var worker in workers)
        {
            _workScheduler.AddWorker(worker.Name, worker);
            _logger.LogInformation(
                "Registered worker: {Namespace}:{Name}",
                worker.Namespace,
                worker.Name);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Registers a worker function programmatically.
    /// </summary>
    /// <param name="workerFunction">The async worker function to execute.</param>
    /// <param name="namespace">Worker namespace.</param>
    /// <param name="name">Worker name.</param>
    /// <param name="maxInProgress">Maximum concurrent executions.</param>
    /// <param name="ioThread">Whether to use IO thread pool.</param>
    public async Task RegisterWorkerFunctionAsync(
        Func<Dictionary<string, object>, Task<object>> workerFunction,
        string @namespace,
        string name,
        int maxInProgress = 10,
        bool ioThread = false)
    {
        if (workerFunction == null)
        {
            throw new ArgumentNullException(nameof(workerFunction));
        }

        var worker = new Worker
        {
            Name = name,
            Namespace = @namespace,
            MaxInProgress = maxInProgress,
            IoThread = ioThread,
            Function = workerFunction
        };

        _registrationClient.AddWorkers(new List<Worker> { worker });
        _workScheduler.AddWorker(name, worker);

        _logger.LogInformation(
            "Registered worker function: {Namespace}:{Name}",
            @namespace,
            name);

        await Task.CompletedTask;
    }

    #endregion

    #region Process Management

    // Process Execution
    
    /// <summary>
    /// Runs a process synchronously and waits for completion.
    /// </summary>
    public async Task<ProcessData> RunProcessSyncAsync(
        ProcessRequestData request,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.RunProcessSyncAsync(request, cancellationToken);
    }

    /// <summary>
    /// Runs a process asynchronously without waiting for completion.
    /// </summary>
    public async Task<ProcessData> RunProcessAsyncAsync(
        ProcessRequestData request,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.RunProcessAsyncAsync(request, cancellationToken);
    }

    /// <summary>
    /// Reruns a process with an optional version.
    /// </summary>
    public async Task<ProcessData> RerunAsync(
        long processId,
        int? version = null,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.RerunAsync(processId, version, cancellationToken);
    }

    // Process Data Retrieval
    
    /// <summary>
    /// Gets process data by ID.
    /// </summary>
    public async Task<ProcessData> GetProcessDataAsync(
        long processId,
        bool includeSteps = false,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.GetProcessDataAsync(processId, includeSteps, cancellationToken);
    }

    /// <summary>
    /// Gets step data by ID.
    /// </summary>
    public async Task<StepData> GetStepDataAsync(
        long stepId,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.GetStepDataAsync(stepId, cancellationToken);
    }

    /// <summary>
    /// Searches for process executions based on criteria.
    /// </summary>
    public async Task<List<ProcessData>> SearchProcessExecutionsAsync(
        ProcessSearchRequest searchRequest,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.SearchProcessExecutionsAsync(searchRequest, cancellationToken);
    }

    // Bulk Process Actions
    
    /// <summary>
    /// Terminates multiple processes in bulk.
    /// </summary>
    public async Task<ProcessActionResponseData> BulkTerminateAsync(
        List<long> processIds,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.BulkTerminateAsync(processIds, reason, cancellationToken);
    }

    /// <summary>
    /// Resumes multiple processes in bulk.
    /// </summary>
    public async Task<ProcessActionResponseData> BulkResumeAsync(
        List<long> processIds,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.BulkResumeAsync(processIds, cancellationToken);
    }

    /// <summary>
    /// Marks multiple processes as reviewed in bulk.
    /// </summary>
    public async Task<ProcessActionResponseData> BulkReviewedAsync(
        List<long> processIds,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.BulkReviewedAsync(processIds, reason, cancellationToken);
    }

    // Process Manipulation
    
    /// <summary>
    /// Removes a step from a process.
    /// </summary>
    public async Task<Dictionary<string, object>> RemoveStepFromProcessAsync(
        long processId,
        long stepId,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.RemoveStepFromProcessAsync(processId, stepId, cancellationToken);
    }

    /// <summary>
    /// Resumes a process with a specific version.
    /// </summary>
    public async Task<Dictionary<string, object>> ResumeWithVersionAsync(
        long processId,
        int version,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.ResumeWithVersionAsync(processId, version, cancellationToken);
    }

    // API Mapping
    
    /// <summary>
    /// Invokes an API mapping with GET method.
    /// </summary>
    public async Task<System.Text.Json.Nodes.JsonNode?> InvokeApiMappingGetAsync(
        string endpoint,
        string? requestId = null,
        string? correlationId = null,
        ApiCallType callType = ApiCallType.ASYNC,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.InvokeApiMappingGetAsync(endpoint, requestId, correlationId, callType, cancellationToken);
    }

    /// <summary>
    /// Invokes an API mapping with POST method.
    /// </summary>
    public async Task<System.Text.Json.Nodes.JsonNode?> InvokeApiMappingPostAsync(
        string endpoint,
        Dictionary<string, object> input,
        string? requestId = null,
        string? correlationId = null,
        ApiCallType callType = ApiCallType.ASYNC,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.InvokeApiMappingPostAsync(endpoint, input, requestId, correlationId, callType, cancellationToken);
    }

    // Process Definition Management
    
    /// <summary>
    /// Gets a process definition by namespace, name, and optional version.
    /// </summary>
    public async Task<ProcessDefinition> GetProcessDefinitionAsync(
        string @namespace,
        string name,
        int? version = null,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.GetProcessDefinitionAsync(@namespace, name, version, cancellationToken);
    }

    /// <summary>
    /// Gets all process definitions.
    /// </summary>
    public async Task<List<ProcessDefinition>> GetAllProcessDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _processClient.GetAllProcessDefinitionsAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new process definition.
    /// </summary>
    public async Task<ProcessDefinition> CreateProcessDefinitionAsync(
        ProcessDefinition processDefinition,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.CreateProcessDefinitionAsync(processDefinition, cancellationToken);
    }

    /// <summary>
    /// Updates an existing process definition.
    /// </summary>
    public async Task<ProcessDefinition> UpdateProcessDefinitionAsync(
        ProcessDefinition processDefinition,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.UpdateProcessDefinitionAsync(processDefinition, cancellationToken);
    }

    /// <summary>
    /// Deletes process definitions.
    /// </summary>
    public async Task<object> DeleteProcessDefinitionsAsync(
        List<ProcessDefinition> processDefinitions,
        bool? versionOnly = null,
        CancellationToken cancellationToken = default)
    {
        return await _processClient.DeleteProcessDefinitionsAsync(processDefinitions, versionOnly, cancellationToken);
    }

    #endregion


    #region Polling and Execution

    /// <summary>
    /// Starts the client and begins polling for work.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            _logger.LogWarning("Client is already started");
            return;
        }

        var workers = _registrationClient.GetWorkers();
        if (workers.Count == 0)
        {
            _logger.LogError("No workers configured. Cannot start polling.");
            throw new InvalidOperationException("No workers registered. Register workers before starting.");
        }
        
        if (!_config.EnableResultsSubmission)
        {
            _logger.LogWarning("Batch processing is disabled for results submission");
            return;
        }

        // Initialize poll states for each worker
        foreach (var worker in workers)
        {
            var pollState = new StepQueuePollState(
                worker,
                new SemaphoreSlim(worker.MaxInProgress, worker.MaxInProgress),
                worker.MaxInProgress);

            _pollStates[worker.FormattedId] = pollState;
        }

        // Renew registration with retry
        await RenewRegistrationWithRetryAsync(cancellationToken);

        // Start polling task
        _pollingTask = Task.Run(() => PollingLoopAsync(_pollingCts.Token), _pollingCts.Token);
        _isStarted = true;

        _logger.LogInformation("Unmeshed SDK started successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Stops the client and cancels all polling operations.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isStarted)
        {
            return;
        }

        _logger.LogInformation("Stopping Unmeshed SDK...");
        
        _pollingCts.Cancel();
        
        if (_pollingTask != null)
        {
            try
            {
                await _pollingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
        }

        _isStarted = false;
        _logger.LogInformation("Unmeshed SDK stopped");
    }

    /// <summary>
    /// Main polling loop that continuously polls for and executes work.
    /// </summary>
    private async Task PollingLoopAsync(CancellationToken cancellationToken)
    {
        // Initial delay before starting polling
        await Task.Delay(TimeSpan.FromMilliseconds(_config.InitialDelayMillis), cancellationToken);

        var lastLogTime = DateTimeOffset.UtcNow;
        var executingCount = 0;
        var pollingErrorReported = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Build poll sizes from available permits
                var pollSizes = new List<StepSize>();

                foreach (var kvp in _pollStates)
                {
                    var workerId = kvp.Key;
                    var pollState = kvp.Value;
                    var availablePermits = pollState.Semaphore.CurrentCount;
                    var maxToAcquire = Math.Min(
                        _config.WorkRequestBatchSize,
                        Math.Min(5000, availablePermits));

                    if (maxToAcquire > 0)
                    {
                        if (await pollState.Semaphore.WaitAsync(0, cancellationToken))
                        {
                            // Acquire permits
                            var acquired = 1;
                            while (acquired < maxToAcquire && 
                                   await pollState.Semaphore.WaitAsync(0, cancellationToken))
                            {
                                acquired++;
                            }

                            pollState.AcquiredPermits = acquired;

                            pollSizes.Add(new StepSize
                            {
                                StepQueueNameData = new StepQueueNameData
                                {
                                    OrgId = 1,
                                    Namespace = pollState.Worker.Namespace,
                                    StepType = UnmeshedConstants.StepType.Worker,
                                    Name = pollState.Worker.Name
                                },
                                Size = acquired
                            });
                        }
                    }
                }

                // Poll for work
                var workRequests = await _pollerClient.PollAsync(pollSizes, cancellationToken);

                if (pollingErrorReported && workRequests.Count > 0)
                {
                    pollingErrorReported = false;
                    _logger.LogInformation("Polling for work resumed successfully");
                }

                // Release unused permits
                foreach (var pollSize in pollSizes)
                {
                    var workerId = FormattedWorkerId(
                        pollSize.StepQueueNameData.Namespace,
                        pollSize.StepQueueNameData.Name);

                    if (_pollStates.TryGetValue(workerId, out var pollState))
                    {
                        var actualUsed = workRequests.Count(w => 
                            w.StepNamespace == pollSize.StepQueueNameData.Namespace &&
                            w.StepName == pollSize.StepQueueNameData.Name);

                        var toRelease = pollState.AcquiredPermits - actualUsed;
                        if (toRelease > 0)
                        {
                            pollState.Semaphore.Release(toRelease);
                        }

                        pollState.AcquiredPermits = 0;
                    }
                }

                // Execute work requests
                foreach (var workRequest in workRequests)
                {
                    var workerId = FormattedWorkerId(workRequest.StepNamespace, workRequest.StepName);
                    
                    if (!_pollStates.TryGetValue(workerId, out var pollState))
                    {
                        _logger.LogWarning(
                            "Received work for unknown worker: {Namespace}:{Name}",
                            workRequest.StepNamespace,
                            workRequest.StepName);
                        continue;
                    }

                    Interlocked.Increment(ref executingCount);

                    // Execute work asynchronously (fire and forget with proper error handling)
                    _ = ExecuteWorkAsync(workRequest, pollState, cancellationToken)
                        .ContinueWith(_ => Interlocked.Decrement(ref executingCount), 
                            TaskScheduler.Default);
                }

                // Periodic logging
                if ((DateTimeOffset.UtcNow - lastLogTime).TotalSeconds >= 5)
                {
                    var permitInfo = string.Join(", ", _pollStates.Select(kvp =>
                        $"{kvp.Value.Worker.Namespace}:{kvp.Value.Worker.Name} = " +
                        $"[{kvp.Value.Semaphore.CurrentCount}/{kvp.Value.TotalPermits}]"));

                    _logger.LogInformation(
                        "Currently executing: {Executing}, Queued submissions: {Queued} - Permits: {Permits}",
                        executingCount,
                        _submitClient.GetSubmitTrackerSize(),
                        permitInfo);

                    lastLogTime = DateTimeOffset.UtcNow;
                }

                // Small delay between polls
                await Task.Delay(100, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!pollingErrorReported)
                {
                    _logger.LogError(ex, "Error during polling");
                    pollingErrorReported = true;
                }
                else
                {
                    _logger.LogDebug("Polling error continues: {Message}", ex.Message);
                }

                // Back off on error
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Executes a single work request asynchronously.
    /// </summary>
    private async Task ExecuteWorkAsync(
        WorkRequest workRequest,
        StepQueuePollState pollState,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create timeout for step execution
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // Only set timeout if it's a reasonable value (less than max allowed ~49 days)
            // Max value for CancelAfter is Int32.MaxValue milliseconds (~24.8 days)
            if (_config.StepTimeoutMillis > 0 && _config.StepTimeoutMillis < int.MaxValue)
            {
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(_config.StepTimeoutMillis));
            }
            // If timeout is too large or 0, don't set a timeout (effectively infinite)

            // Execute the work
            var result = await _workScheduler.ScheduleAsync(workRequest, timeoutCts.Token);

            // Build success response
            var response = _responseBuilder.SuccessResponse(workRequest, result);
            // Check if we need to send a running response instead
            if ((result.KeepRunning == true) || (result.RescheduleAfterSeconds.HasValue && result.RescheduleAfterSeconds > 0))
            {
                response = _responseBuilder.RunningResponse(workRequest, result);
            }

            response.Output["__workCompletedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Submit response
            await _submitClient.SubmitAsync(response, pollState.Semaphore, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Work execution cancelled for step {StepId}",
                workRequest.StepId);
            
            pollState.Semaphore.Release();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing work for step {StepId}: {Error}",
                workRequest.StepId,
                ex.Message);

            // Build failure response
            var response = _responseBuilder.FailResponse(workRequest, ex, _config);
            response.Output["__workCompletedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Submit failure response
            await _submitClient.SubmitAsync(response, pollState.Semaphore, cancellationToken);
        }
    }

    /// <summary>
    /// Renews registration with retry logic.
    /// </summary>
    private async Task RenewRegistrationWithRetryAsync(CancellationToken cancellationToken)
    {
        int retryCount = 0;
        int delay = 1;
        const int maxDelay = 10;

        while (true)
        {
            try
            {
                _logger.LogInformation("Attempting to renew registration. Retry count: {RetryCount}", retryCount);
                
                await _registrationClient.RenewRegistrationAsync(cancellationToken);
                
                _logger.LogInformation("Registration renewed successfully");
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogError(
                    "Retry {RetryCount} failed: {Error}",
                    retryCount,
                    ex.Message);

                if (retryCount >= 10)
                {
                    _logger.LogError("Failed to register after {RetryCount} attempts", retryCount);
                    throw;
                }

                _logger.LogInformation("Waiting {Delay} seconds before retrying...", delay);
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

                delay = Math.Min(delay + 2, maxDelay);
            }
        }
    }

    #endregion

    #region Helper Methods

    private static string FormattedWorkerId(string @namespace, string name)
    {
        return $"{@namespace}:-#-:{name}";
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the client and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopAsync().GetAwaiter().GetResult();
        _pollingCts.Dispose();

        if (_submitClient is IDisposable disposableSubmit)
        {
            disposableSubmit.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Tracks polling state for a worker.
/// </summary>
internal class StepQueuePollState
{
    public Worker Worker { get; }
    public SemaphoreSlim Semaphore { get; }
    public int TotalPermits { get; }
    public int AcquiredPermits { get; set; }

    public StepQueuePollState(Worker worker, SemaphoreSlim semaphore, int totalPermits)
    {
        Worker = worker;
        Semaphore = semaphore;
        TotalPermits = totalPermits;
        AcquiredPermits = 0;
    }
}
