using Microsoft.Extensions.Logging;
using Unmeshed.Sdk;
using Unmeshed.Sdk.Configuration;
using Unmeshed.Sdk.Exceptions;
using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Examples;

/// <summary>
/// Comprehensive example demonstrating ALL ProcessClient operations.
/// This mirrors the Java SDK's SDKTestProcessManagement example.
/// </summary>
public class ProcessClientExample
{
    private static string GetEnvOrDefault(string key, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }

    public static async Task Main(string[] args)
    {
        // Configure logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<ProcessClientExample>();

        // Get configuration from environment variables
        var authId = GetEnvOrDefault("UNMESHED_AUTH_ID", "<UNMESHED_AUTH_ID>");
        var authToken = GetEnvOrDefault("UNMESHED_AUTH_TOKEN", "<UNMESHED_AUTH_TOKEN>");
        var baseUrl = GetEnvOrDefault("UNMESHED_ENGINE_HOST", "http://localhost");
        var port = int.Parse(GetEnvOrDefault("UNMESHED_ENGINE_PORT", "8080"));

        if (string.IsNullOrWhiteSpace(authId) || string.IsNullOrWhiteSpace(authToken))
        {
            logger.LogError(@"
Required parameters have not been provided. Please ensure you have the following environment variables set:
  * UNMESHED_AUTH_ID
  * UNMESHED_AUTH_TOKEN
  * UNMESHED_ENGINE_HOST (optional, defaults to http://localhost)
  * UNMESHED_ENGINE_PORT (optional, defaults to 8080)");
            Environment.Exit(-1);
        }

        // Create client configuration
        var config = new ClientConfig
        {
            ClientId = authId,
            AuthToken = authToken,
            BaseUrl = baseUrl,
            Port = port,
            WorkRequestBatchSize = 2000,
            InitialDelayMillis = 50,
            ResponseSubmitBatchSize = 2000,
            StepTimeoutMillis = 30000,
            EnableResultsSubmission = true
        };

        // Create Unmeshed client
        using var client = new UnmeshedClient(config, loggerFactory);

        logger.LogInformation("=== Comprehensive ProcessClient Examples ===\n");

        try
        {
            // ===== PROCESS DEFINITION MANAGEMENT =====
            await ProcessDefinitionExamples(client, logger);

            // ===== PROCESS EXECUTION =====
            await ProcessExecutionExamples(client, logger);

            // ===== PROCESS DATA RETRIEVAL =====
            await ProcessDataRetrievalExamples(client, logger);

            // ===== BULK PROCESS ACTIONS =====
            await BulkProcessActionsExamples(client, logger);

            // ===== PROCESS MANIPULATION =====
            await ProcessManipulationExamples(client, logger);

            // ===== PROCESS SEARCH =====
            await ProcessSearchExamples(client, logger);

            // ===== CLEANUP: DELETE PROCESS DEFINITION =====
            await DeleteProcessDefinitionExample(client, logger);

            // ===== API MAPPING =====
            await ApiMappingExamples(client, logger);

            logger.LogInformation("\n=== All examples completed successfully ===");
        }
        catch (UnmeshedApiException ex)
        {
            logger.LogError(ex, "Unmeshed API Error: {StatusCode} - {Content}", ex.StatusCode, ex.ErrorContent);
            Environment.Exit(-1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running examples");
            Environment.Exit(-1);
        }
    }

    /// <summary>
    /// Examples for Process Definition Management (CRUD operations)
    /// </summary>
    private static async Task ProcessDefinitionExamples(UnmeshedClient client, ILogger logger)
    {
        logger.LogInformation("\n--- Process Definition Management Examples ---");

        // Create a new process definition
        var processDefinition = new ProcessDefinition
        {
            Namespace = "default",
            Name = "test_process_v2",
            Version = 1,
            Type = UnmeshedConstants.ProcessType.ApiOrchestration,
            Description = "Test unmeshed process created by C# SDK",
            Steps = new List<StepDefinition>
            {
                new StepDefinition
                {
                    Name = "test_noop",
                    Ref = "test_noop_ref",
                    Type = UnmeshedConstants.StepType.Noop,
                    Namespace = "default",
                    Input = new Dictionary<string, object> { { "key1", "val1" } }
                }
            }
        };

        logger.LogInformation("Creating process definition...");
        var created = await client.CreateProcessDefinitionAsync(processDefinition);
        logger.LogInformation("Created: {Namespace}:{Name} v{Version}", created.Namespace, created.Name, created.Version);

        // Get process definition
        logger.LogInformation("\nFetching process definition...");
        var fetched = await client.GetProcessDefinitionAsync("default", "test_process_v2", version: null);
        logger.LogInformation("Fetched: {Namespace}:{Name} v{Version}", fetched.Namespace, fetched.Name, fetched.Version);

        // Get all process definitions
        logger.LogInformation("\nFetching all process definitions...");
        var all = await client.GetAllProcessDefinitionsAsync();
        logger.LogInformation("Found {Count} process definitions", all.Count);

        // Update process definition
        var updated = new ProcessDefinition
        {
            Namespace = "default",
            Name = "test_process_v2",
            Version = 2,
            Type = UnmeshedConstants.ProcessType.ApiOrchestration,
            Description = "Updated test unmeshed process created by C# SDK",
            Steps = new List<StepDefinition>
            {
                new StepDefinition
                {
                    Name = "test_noop",
                    Ref = "test_noop_ref",
                    Type = UnmeshedConstants.StepType.Noop,
                    Namespace = "default",
                    Input = new Dictionary<string, object> { { "key1", "val1" } }
                },
                new StepDefinition
                {
                    Name = "test_noop_2",
                    Ref = "test_noop_ref_2",
                    Type = UnmeshedConstants.StepType.Noop,
                    Namespace = "default",
                    Input = new Dictionary<string, object> { { "key2", "val2" } }
                }
            }
        };

        logger.LogInformation("\nUpdating process definition...");
        var updatedResult = await client.UpdateProcessDefinitionAsync(updated);
        logger.LogInformation("Updated: {Namespace}:{Name} v{Version} with {StepCount} steps", 
            updatedResult.Namespace, updatedResult.Name, updatedResult.Version, updatedResult.Steps.Count);

        // Note: We don't delete the process definition here as it's needed for subsequent examples
        // Delete operation can be performed manually or at the end of all examples if needed
    }

    /// <summary>
    /// Examples for Process Execution
    /// </summary>
    private static async Task ProcessExecutionExamples(UnmeshedClient client, ILogger logger)
    {
        logger.LogInformation("\n--- Process Execution Examples ---");

        // Run process synchronously
        var syncRequest = new ProcessRequestData
        {
            Namespace = "default",
            Name = "test_process_v2",
            Input = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 }
            },
            CorrelationId = Guid.NewGuid().ToString(),
            RequestId = Guid.NewGuid().ToString()
        };

        logger.LogInformation("Running process synchronously...");
        var syncResult = await client.RunProcessSyncAsync(syncRequest);
        logger.LogInformation("Sync process completed: ProcessId={ProcessId}, Status={Status}", 
            syncResult.ProcessId, syncResult.Status);

        // Run process asynchronously
        var asyncRequest = new ProcessRequestData
        {
            Namespace = "default",
            Name = "test_process_v2",
            Input = new Dictionary<string, object> { { "duration", 10 } }
        };

        logger.LogInformation("\nRunning process asynchronously...");
        var asyncResult = await client.RunProcessAsyncAsync(asyncRequest);
        logger.LogInformation("Async process started: ProcessId={ProcessId}, Status={Status}", 
            asyncResult.ProcessId, asyncResult.Status);

        // Rerun a process
        logger.LogInformation("\nRerunning process...");
        var rerunResult = await client.RerunAsync(syncResult.ProcessId, version: null);
        logger.LogInformation("Process rerun: ProcessId={ProcessId}, Status={Status}", 
            rerunResult.ProcessId, rerunResult.Status);
    }

    /// <summary>
    /// Examples for Process Data Retrieval
    /// </summary>
    private static async Task ProcessDataRetrievalExamples(UnmeshedClient client, ILogger logger)
    {
        logger.LogInformation("\n--- Process Data Retrieval Examples ---");

        // Create a test process first
        var request = new ProcessRequestData
        {
            Namespace = "default",
            Name = "test_process_v2",
            Input = new Dictionary<string, object> { { "test", "data" } }
        };

        var process = await client.RunProcessAsyncAsync(request);
        logger.LogInformation("Created test process: ProcessId={ProcessId}", process.ProcessId);

        // Get process data without steps
        logger.LogInformation("\nFetching process data (without steps)...");
        var processData = await client.GetProcessDataAsync(process.ProcessId, includeSteps: false);
        logger.LogInformation("Process: {Namespace}:{Name}, Status={Status}", 
            processData.Namespace, processData.Name, processData.Status);

        // Get process data with steps
        logger.LogInformation("\nFetching process data (with steps)...");
        var processDataWithSteps = await client.GetProcessDataAsync(process.ProcessId, includeSteps: true);
        logger.LogInformation("Process with steps retrieved");

        // Get step data (if there are steps)
        // Note: This would require a valid step ID from an actual process
        // logger.LogInformation("\nFetching step data...");
        // var stepData = await client.GetStepDataAsync(stepId);
        // logger.LogInformation("Step: {Name}, Status={Status}", stepData.Name, stepData.Status);
    }

    /// <summary>
    /// Examples for Bulk Process Actions
    /// </summary>
    private static async Task BulkProcessActionsExamples(UnmeshedClient client, ILogger logger)
    {
        logger.LogInformation("\n--- Bulk Process Actions Examples ---");

        // Create multiple processes for bulk operations
        var processIds = new List<long>();
        for (int i = 0; i < 3; i++)
        {
            var request = new ProcessRequestData
            {
                Namespace = "default",
                Name = "test_process_v2",
                Input = new Dictionary<string, object> { { "index", i } }
            };
            var process = await client.RunProcessAsyncAsync(request);
            processIds.Add(process.ProcessId);
        }

        logger.LogInformation("Created {Count} processes for bulk operations: {ProcessIds}", 
            processIds.Count, string.Join(", ", processIds));

        // Bulk Resume
        logger.LogInformation("\nBulk resuming processes...");
        var resumeResult = await client.BulkResumeAsync(processIds);
        logger.LogInformation("Bulk resume: Success={Success}, Failed={Failed}", 
            resumeResult.SuccessCount, resumeResult.FailureCount);

        // Bulk Reviewed
        logger.LogInformation("\nBulk marking processes as reviewed...");
        var reviewedResult = await client.BulkReviewedAsync(processIds, reason: "Reviewed by C# SDK example");
        logger.LogInformation("Bulk reviewed: Success={Success}, Failed={Failed}", 
            reviewedResult.SuccessCount, reviewedResult.FailureCount);

        // Bulk Terminate
        logger.LogInformation("\nBulk terminating processes...");
        var terminateResult = await client.BulkTerminateAsync(processIds, reason: "Terminated by C# SDK example");
        logger.LogInformation("Bulk terminate: Success={Success}, Failed={Failed}", 
            terminateResult.SuccessCount, terminateResult.FailureCount);
    }

    /// <summary>
    /// Examples for Process Manipulation
    /// </summary>
    private static async Task ProcessManipulationExamples(UnmeshedClient client, ILogger logger)
    {
        logger.LogInformation("\n--- Process Manipulation Examples ---");

        // Create a test process
        var request = new ProcessRequestData
        {
            Namespace = "default",
            Name = "test_process_v2",
            Input = new Dictionary<string, object> { { "test", "manipulation" } }
        };

        var process = await client.RunProcessAsyncAsync(request);
        logger.LogInformation("Created process for manipulation: ProcessId={ProcessId}", process.ProcessId);

        // Resume with version
        logger.LogInformation("\nResuming process with version...");
        var resumeResult = await client.ResumeWithVersionAsync(process.ProcessId, version: 1);
        logger.LogInformation("Resume with version result: {Result}", resumeResult.Count);

        // Remove step from process (requires valid step ID)
        // logger.LogInformation("\nRemoving step from process...");
        // var removeResult = await client.RemoveStepFromProcessAsync(process.ProcessId, stepId);
        // logger.LogInformation("Remove step result: {Result}", removeResult.Count);
    }

    /// <summary>
    /// Examples for Process Search
    /// </summary>
    private static async Task ProcessSearchExamples(UnmeshedClient client, ILogger logger)
    {
        logger.LogInformation("\n--- Process Search Examples ---");

        // Search by namespace
        var searchRequest = new ProcessSearchRequest
        {
            Namespace = "default",
            Limit = 10,
            Offset = 0
        };

        logger.LogInformation("Searching processes in namespace 'default'...");
        var searchResults = await client.SearchProcessExecutionsAsync(searchRequest);
        logger.LogInformation("Found {Count} processes", searchResults.Count);

        // Search by time range
        var timeRangeSearch = new ProcessSearchRequest
        {
            StartTimeEpoch = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds(),
            EndTimeEpoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Limit = 20
        };

        logger.LogInformation("\nSearching processes from last 24 hours...");
        var timeRangeResults = await client.SearchProcessExecutionsAsync(timeRangeSearch);
        logger.LogInformation("Found {Count} processes in time range", timeRangeResults.Count);

        // Search by status
        var statusSearch = new ProcessSearchRequest
        {
            Namespace = "default",
            Statuses = new List<string> { UnmeshedConstants.ProcessStatus.Completed, UnmeshedConstants.ProcessStatus.Running },
            Limit = 15
        };

        logger.LogInformation("\nSearching processes by status...");
        var statusResults = await client.SearchProcessExecutionsAsync(statusSearch);
        logger.LogInformation("Found {Count} processes with specified statuses", statusResults.Count);
    }

    /// <summary>
    /// Example for deleting the test process definition
    /// </summary>
    private static async Task DeleteProcessDefinitionExample(UnmeshedClient client, ILogger logger)
    {
        logger.LogInformation("\n--- Delete Process Definition Example ---");

        try
        {
            // Get the process definition to delete
            logger.LogInformation("Fetching test_process_v2 for deletion...");
            var processDefToDelete = await client.GetProcessDefinitionAsync("default", "test_process_v2", version: null);
            
            logger.LogInformation("Deleting process definition: {Namespace}:{Name} v{Version}", 
                processDefToDelete.Namespace, processDefToDelete.Name, processDefToDelete.Version);
            
            var deleteResult = await client.DeleteProcessDefinitionsAsync(
                new List<ProcessDefinition> { processDefToDelete }, 
                versionOnly: null);
            
            logger.LogInformation("Successfully deleted process definition");
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to delete process definition: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Examples for API Mapping
    /// </summary>
    private static async Task ApiMappingExamples(UnmeshedClient client, ILogger logger)
    {
        logger.LogInformation("\n--- API Mapping Examples ---");

        // API Mapping GET - using test_process_v2 endpoint
        try
        {
            logger.LogInformation("Invoking API mapping GET for test_process_v2...");
            var getResult = await client.InvokeApiMappingGetAsync(
                endpoint: "endpoint/tayprq8YjCPNQF5I3oHc/Tf4K9pKwlA8JC8gbux4I",
                requestId: Guid.NewGuid().ToString(),
                correlationId: Guid.NewGuid().ToString(),
                callType: ApiCallType.ASYNC
            );
            logger.LogInformation("API GET result: {Result}", getResult?.ToJsonString());
        }
        catch (Exception ex)
        {
            logger.LogWarning("API GET example failed (endpoint may not exist): {Message}", ex.Message);
        }

        // API Mapping POST - using test_process_v2 endpoint
        try
        {
            logger.LogInformation("\nInvoking API mapping POST for test_process_v2...");
            var postInput = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 123 },
                { "key3", true }
            };

            var postResult = await client.InvokeApiMappingPostAsync(
                endpoint: "endpoint/tayprq8YjCPNQF5I3oHc/Tf4K9pKwlA8JC8gbux4I",
                input: postInput,
                requestId: Guid.NewGuid().ToString(),
                correlationId: Guid.NewGuid().ToString(),
                callType: ApiCallType.ASYNC
            );
            logger.LogInformation("API POST result: {Result}", postResult?.ToJsonString());
        }
        catch (Exception ex)
        {
            logger.LogWarning("API POST example failed (endpoint may not exist): {Message}", ex.Message);
        }
    }
}
