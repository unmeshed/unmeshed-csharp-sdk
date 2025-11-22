using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Unmeshed.Sdk.Configuration;
using Unmeshed.Sdk.Models;
using Unmeshed.Sdk.Workers;

namespace Unmeshed.Sdk.Schedulers;

/// <summary>
/// Schedules and executes worker tasks asynchronously.
/// </summary>
public class WorkScheduler : IWorkScheduler
{
    private readonly ClientConfig _config;
    private readonly ILogger<WorkScheduler> _logger;
    private readonly ConcurrentDictionary<string, Worker> _workers;
    private readonly TaskScheduler _ioTaskScheduler;
    private readonly TaskScheduler _cpuTaskScheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkScheduler"/> class.
    /// </summary>
    public WorkScheduler(ClientConfig config, ILoggerFactory loggerFactory)
    {
        _config = config;
        _logger = loggerFactory.CreateLogger<WorkScheduler>();
        _workers = new ConcurrentDictionary<string, Worker>();

        // Create separate task schedulers for IO and CPU-bound work
        _ioTaskScheduler = TaskScheduler.Default;
        _cpuTaskScheduler = new LimitedConcurrencyLevelTaskScheduler(_config.FixedThreadPoolSize);
    }

    /// <summary>
    /// Adds a worker to the scheduler.
    /// </summary>
    public void AddWorker(string workerStepName, Worker worker)
    {
        _workers[workerStepName] = worker;
        _logger.LogInformation(
            "Registered worker: {Namespace}:{Name} (MaxInProgress: {MaxInProgress}, IoThread: {IoThread})",
            worker.Namespace,
            worker.Name,
            worker.MaxInProgress,
            worker.IoThread);
    }

    /// <summary>
    /// Schedules a work request for execution asynchronously.
    /// </summary>
    public async Task<StepResult> ScheduleAsync(
        WorkRequest workRequest,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            if (!_workers.TryGetValue(workRequest.StepName, out var worker))
            {
                throw new InvalidOperationException(
                    $"No worker registered for step: {workRequest.StepName}");
            }

            _logger.LogDebug(
                "Executing worker {StepName} for process {ProcessId}, step {StepId}",
                workRequest.StepName,
                workRequest.ProcessId,
                workRequest.StepId);

            // Choose appropriate task scheduler
            var taskScheduler = worker.IoThread ? _ioTaskScheduler : _cpuTaskScheduler;

            // Execute the worker function
            object? result;
            if (worker.Function != null)
            {
                // Use registered function
                result = await Task.Factory.StartNew(
                    async () => await worker.Function(workRequest.Input ?? new Dictionary<string, object>()),
                    cancellationToken,
                    TaskCreationOptions.DenyChildAttach,
                    taskScheduler).Unwrap();
            }
            else if (worker.Method != null)
            {
                // Use reflection to invoke method
                result = await InvokeWorkerMethodAsync(worker, workRequest.Input, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Worker {workRequest.StepName} has no execution method or function");
            }

            // Convert result to StepResult
            var stepResult = ConvertToStepResult(result, startedAt);

            _logger.LogDebug(
                "Worker {StepName} completed successfully for step {StepId}",
                workRequest.StepName,
                workRequest.StepId);

            return stepResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Worker {StepName} failed for step {StepId}: {Error}",
                workRequest.StepName,
                workRequest.StepId,
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Invokes a worker method using reflection.
    /// </summary>
    private async Task<object?> InvokeWorkerMethodAsync(
        Worker worker,
        Dictionary<string, object>? input,
        CancellationToken cancellationToken)
    {
        var method = worker.Method!;
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        // Prepare method arguments
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            if (paramType == typeof(Dictionary<string, object>) || paramType == typeof(IDictionary<string, object>))
            {
                args[i] = input ?? new Dictionary<string, object>();
            }
            else if (paramType == typeof(CancellationToken))
            {
                args[i] = cancellationToken;
            }
            else
            {
                args[i] = null;
            }
        }

        try
        {
            // Invoke method
            var result = method.Invoke(worker.Instance, args);

            // Handle async methods
            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                // Get result from Task<T>
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task);
            }

            return result;
        }
        catch (TargetInvocationException ex)
        {
            // Unwrap the actual exception from the worker
            if (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            throw;
        }
    }

    /// <summary>
    /// Converts worker result to StepResult.
    /// </summary>
    private StepResult ConvertToStepResult(object? result, long startedAt)
    {
        var completedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (result is StepResult stepResult)
        {
            stepResult.StartedAt = startedAt;
            stepResult.CompletedAt = completedAt;
            return stepResult;
        }

        // Convert to dictionary if possible
        Dictionary<string, object> output;

        if (result is Dictionary<string, object> dict)
        {
            output = dict;
        }
        else if (result != null)
        {
            // Try to convert object to dictionary using System.Text.Json
            try
            {
                var jsonElement = System.Text.Json.JsonSerializer.SerializeToElement(result);
                
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var json = jsonElement.GetRawText();
                    // Use our custom converter to ensure correct types
                    var options = new System.Text.Json.JsonSerializerOptions();
                    options.Converters.Add(new Unmeshed.Sdk.Serialization.DictionaryStringObjectJsonConverter());
                    
                    output = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json, options) 
                             ?? new Dictionary<string, object>();
                }
                else
                {
                    // Primitive or array, wrap in result
                    output = new Dictionary<string, object> { { "result", result } };
                }
            }
            catch
            {
                // Fallback
                output = new Dictionary<string, object> { { "result", result } };
            }
        }
        else
        {
            output = new Dictionary<string, object>();
        }

        return new StepResult
        {
            Output = output,
            StartedAt = startedAt,
            CompletedAt = completedAt
        };
    }
}

/// <summary>
/// Task scheduler with limited concurrency.
/// </summary>
internal class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
{
    private readonly int _maxDegreeOfParallelism;
    private readonly LinkedList<Task> _tasks = new();
    private int _delegatesQueuedOrRunning = 0;

    public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism < 1)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    protected override void QueueTask(Task task)
    {
        lock (_tasks)
        {
            _tasks.AddLast(task);

            if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
            {
                ++_delegatesQueuedOrRunning;
                NotifyThreadPoolOfPendingWork();
            }
        }
    }

    private void NotifyThreadPoolOfPendingWork()
    {
        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            try
            {
                while (true)
                {
                    Task? item;
                    lock (_tasks)
                    {
                        if (_tasks.Count == 0)
                        {
                            --_delegatesQueuedOrRunning;
                            break;
                        }

                        item = _tasks.First!.Value;
                        _tasks.RemoveFirst();
                    }

                    TryExecuteTask(item);
                }
            }
            finally
            {
                // Ensure we decrement if an exception occurs
            }
        }, null);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        return false; // Don't allow inline execution
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        lock (_tasks)
        {
            return _tasks.ToArray();
        }
    }

    public override int MaximumConcurrencyLevel => _maxDegreeOfParallelism;
}
