using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Unmeshed.Sdk.Configuration;
using Unmeshed.Sdk.Http;
using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Process;

/// <summary>
/// Interface for process management operations.
/// </summary>
public interface IProcessClient
{
    // Process Execution
    
    /// <summary>Runs a process synchronously and waits for completion.</summary>
    Task<ProcessData> RunProcessSyncAsync(ProcessRequestData processRequestData, CancellationToken cancellationToken = default);
    
    /// <summary>Runs a process asynchronously without waiting for completion.</summary>
    Task<ProcessData> RunProcessAsyncAsync(ProcessRequestData processRequestData, CancellationToken cancellationToken = default);
    
    /// <summary>Reruns a process with an optional version.</summary>
    Task<ProcessData> RerunAsync(long processId, int? version = null, CancellationToken cancellationToken = default);
    
    // Process Data Retrieval
    
    /// <summary>Gets process data by ID.</summary>
    Task<ProcessData> GetProcessDataAsync(long processId, bool includeSteps = false, CancellationToken cancellationToken = default);
    
    /// <summary>Gets step data by ID.</summary>
    Task<StepData> GetStepDataAsync(long stepId, CancellationToken cancellationToken = default);
    
    /// <summary>Searches for process executions based on criteria.</summary>
    Task<List<ProcessData>> SearchProcessExecutionsAsync(ProcessSearchRequest searchRequest, CancellationToken cancellationToken = default);
    
    // Bulk Process Actions
    
    /// <summary>Terminates multiple processes in bulk.</summary>
    Task<ProcessActionResponseData> BulkTerminateAsync(List<long> processIds, string? reason = null, CancellationToken cancellationToken = default);
    
    /// <summary>Resumes multiple processes in bulk.</summary>
    Task<ProcessActionResponseData> BulkResumeAsync(List<long> processIds, CancellationToken cancellationToken = default);
    
    /// <summary>Marks multiple processes as reviewed in bulk.</summary>
    Task<ProcessActionResponseData> BulkReviewedAsync(List<long> processIds, string? reason = null, CancellationToken cancellationToken = default);
    
    // Process Manipulation
    
    /// <summary>Removes a step from a process.</summary>
    Task<Dictionary<string, object>> RemoveStepFromProcessAsync(long processId, long stepId, CancellationToken cancellationToken = default);
    
    /// <summary>Resumes a process with a specific version.</summary>
    Task<Dictionary<string, object>> ResumeWithVersionAsync(long processId, int version, CancellationToken cancellationToken = default);
    
    // API Mapping
    
    /// <summary>Invokes an API mapping with GET method.</summary>
    Task<JsonNode?> InvokeApiMappingGetAsync(string endpoint, string? requestId = null, string? correlationId = null, ApiCallType callType = ApiCallType.ASYNC, CancellationToken cancellationToken = default);
    
    /// <summary>Invokes an API mapping with POST method.</summary>
    Task<JsonNode?> InvokeApiMappingPostAsync(string endpoint, Dictionary<string, object> input, string? requestId = null, string? correlationId = null, ApiCallType callType = ApiCallType.ASYNC, CancellationToken cancellationToken = default);
    
    // Process Definition Management
    
    /// <summary>Gets a process definition by namespace, name, and optional version.</summary>
    Task<ProcessDefinition> GetProcessDefinitionAsync(string @namespace, string name, int? version = null, CancellationToken cancellationToken = default);
    
    /// <summary>Gets all process definitions.</summary>
    Task<List<ProcessDefinition>> GetAllProcessDefinitionsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Creates a new process definition.</summary>
    Task<ProcessDefinition> CreateProcessDefinitionAsync(ProcessDefinition processDefinition, CancellationToken cancellationToken = default);
    
    /// <summary>Updates an existing process definition.</summary>
    Task<ProcessDefinition> UpdateProcessDefinitionAsync(ProcessDefinition processDefinition, CancellationToken cancellationToken = default);
    
    /// <summary>Deletes process definitions.</summary>
    Task<object> DeleteProcessDefinitionsAsync(List<ProcessDefinition> processDefinitions, bool? versionOnly = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles process-related operations with the Unmeshed engine.
/// </summary>
public class ProcessClient : IProcessClient
{
    private readonly HttpClient _httpClient;
    private readonly ClientConfig _config;
    private readonly ILogger<ProcessClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessClient"/> class.
    /// </summary>
    public ProcessClient(
        Http.IHttpClientFactory httpClientFactory,
        ClientConfig config,
        ILoggerFactory loggerFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
        _logger = loggerFactory.CreateLogger<ProcessClient>();
    }

    #region Process Execution

    /// <summary>
    /// Runs a process synchronously and waits for completion.
    /// </summary>
    public async Task<ProcessData> RunProcessSyncAsync(
        ProcessRequestData request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Running process synchronously: {Namespace}:{Name}",
                request.Namespace,
                request.Name);

            var response = await _httpClient.PostAsJsonAsync(
                "api/process/runSync",
                request,
                cancellationToken);

            await response.EnsureSuccessStatusCodeWithContentAsync();

            var processData = await response.Content.ReadFromJsonAsync<ProcessData>(
                cancellationToken: cancellationToken);

            if (processData == null)
            {
                throw new InvalidOperationException("Failed to deserialize process data");
            }

            _logger.LogInformation(
                "Process completed: {ProcessId}, Status: {Status}",
                processData.ProcessId,
                processData.Status);

            return processData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running process synchronously");
            throw;
        }
    }

    /// <summary>
    /// Runs a process asynchronously without waiting for completion.
    /// </summary>
    public async Task<ProcessData> RunProcessAsyncAsync(
        ProcessRequestData request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Running process asynchronously: {Namespace}:{Name}",
                request.Namespace,
                request.Name);

            var response = await _httpClient.PostAsJsonAsync(
                "api/process/runAsync",
                request,
                cancellationToken);

            await response.EnsureSuccessStatusCodeWithContentAsync();

            var processData = await response.Content.ReadFromJsonAsync<ProcessData>(
                cancellationToken: cancellationToken);

            if (processData == null)
            {
                throw new InvalidOperationException("Failed to deserialize process data");
            }

            _logger.LogInformation(
                "Process started: {ProcessId}",
                processData.ProcessId);

            return processData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running process asynchronously");
            throw;
        }
    }

    /// <summary>
    /// Reruns a process with an optional version.
    /// </summary>
    public async Task<ProcessData> RerunAsync(
        long processId,
        int? version = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Rerunning process {ProcessId} with version {Version}", processId, version);

            var url = $"api/process/rerun?clientId={_config.ClientId}&processId={processId}";
            if (version.HasValue)
            {
                url += $"&version={version.Value}";
            }

            var response = await _httpClient.PostAsync(url, null, cancellationToken);
            await response.EnsureSuccessStatusCodeWithContentAsync();

            var processData = await response.Content.ReadFromJsonAsync<ProcessData>(
                cancellationToken: cancellationToken);

            if (processData == null)
            {
                throw new InvalidOperationException("Failed to deserialize process data");
            }

            return processData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rerunning process {ProcessId}", processId);
            throw;
        }
    }

    #endregion

    #region Process Data Retrieval

    /// <summary>
    /// Gets process data by ID.
    /// </summary>
    public async Task<ProcessData> GetProcessDataAsync(
        long processId,
        bool includeSteps,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting process data for {ProcessId}", processId);

            var url = $"api/process/context/{processId}?includeSteps={includeSteps}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            await response.EnsureSuccessStatusCodeWithContentAsync();

            var processData = await response.Content.ReadFromJsonAsync<ProcessData>(
                cancellationToken: cancellationToken);

            if (processData == null)
            {
                throw new InvalidOperationException("Failed to deserialize process data");
            }

            return processData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting process data for {ProcessId}", processId);
            throw;
        }
    }

    /// <summary>
    /// Gets step data by ID.
    /// </summary>
    public async Task<StepData> GetStepDataAsync(
        long stepId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting step data for {StepId}", stepId);

            var url = $"api/process/stepContext/{stepId}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            await response.EnsureSuccessStatusCodeWithContentAsync();

            var stepData = await response.Content.ReadFromJsonAsync<StepData>(
                cancellationToken: cancellationToken);

            if (stepData == null)
            {
                throw new InvalidOperationException("Failed to deserialize step data");
            }

            return stepData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting step data for {StepId}", stepId);
            throw;
        }
    }

    /// <summary>
    /// Searches for process executions based on criteria.
    /// </summary>
    public async Task<List<ProcessData>> SearchProcessExecutionsAsync(
        ProcessSearchRequest searchRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Searching process executions");

            var queryParams = new List<string>();

            if (searchRequest.StartTimeEpoch.HasValue)
                queryParams.Add($"startTimeEpoch={searchRequest.StartTimeEpoch.Value}");
            if (searchRequest.EndTimeEpoch.HasValue)
                queryParams.Add($"endTimeEpoch={searchRequest.EndTimeEpoch.Value}");
            if (!string.IsNullOrEmpty(searchRequest.Namespace))
                queryParams.Add($"namespace={Uri.EscapeDataString(searchRequest.Namespace)}");
            if (searchRequest.ProcessTypes?.Any() == true)
                queryParams.Add($"processTypes={string.Join(",", searchRequest.ProcessTypes)}");
            if (searchRequest.TriggerTypes?.Any() == true)
                queryParams.Add($"triggerTypes={string.Join(",", searchRequest.TriggerTypes)}");
            if (searchRequest.Names?.Any() == true)
                queryParams.Add($"names={string.Join(",", searchRequest.Names.Select(Uri.EscapeDataString))}");
            if (searchRequest.ProcessIds?.Any() == true)
                queryParams.Add($"processIds={string.Join(",", searchRequest.ProcessIds)}");
            if (searchRequest.CorrelationIds?.Any() == true)
                queryParams.Add($"correlationIds={string.Join(",", searchRequest.CorrelationIds.Select(Uri.EscapeDataString))}");
            if (searchRequest.RequestIds?.Any() == true)
                queryParams.Add($"requestIds={string.Join(",", searchRequest.RequestIds.Select(Uri.EscapeDataString))}");
            if (searchRequest.Statuses?.Any() == true)
                queryParams.Add($"statuses={string.Join(",", searchRequest.Statuses)}");
            if (searchRequest.Limit > 0)
                queryParams.Add($"limit={searchRequest.Limit}");
            if (searchRequest.Offset > 0)
                queryParams.Add($"offset={searchRequest.Offset}");

            var url = $"api/stats/process/search?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            await response.EnsureSuccessStatusCodeWithContentAsync();

            var processes = await response.Content.ReadFromJsonAsync<List<ProcessData>>(
                cancellationToken: cancellationToken);

            return processes ?? new List<ProcessData>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching process executions");
            throw;
        }
    }

    #endregion

    #region Bulk Process Actions

    /// <summary>
    /// Terminates multiple processes in bulk.
    /// </summary>
    public async Task<ProcessActionResponseData> BulkTerminateAsync(
        List<long> processIds,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bulk terminating {Count} processes", processIds.Count);

            var url = "api/process/bulkTerminate";
            if (!string.IsNullOrEmpty(reason))
            {
                url += $"?reason={Uri.EscapeDataString(reason)}";
            }

            var response = await _httpClient.PostAsJsonAsync(url, processIds, cancellationToken);
            await response.EnsureSuccessStatusCodeWithContentAsync();

            var result = await response.Content.ReadFromJsonAsync<ProcessActionResponseData>(
                cancellationToken: cancellationToken);

            return result ?? new ProcessActionResponseData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk terminating processes");
            throw;
        }
    }

    /// <summary>
    /// Resumes multiple processes in bulk.
    /// </summary>
    public async Task<ProcessActionResponseData> BulkResumeAsync(
        List<long> processIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bulk resuming {Count} processes", processIds.Count);

            var url = $"api/process/bulkResume?clientId={_config.ClientId}";
            var response = await _httpClient.PostAsJsonAsync(url, processIds, cancellationToken);
            await response.EnsureSuccessStatusCodeWithContentAsync();

            var result = await response.Content.ReadFromJsonAsync<ProcessActionResponseData>(
                cancellationToken: cancellationToken);

            return result ?? new ProcessActionResponseData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk resuming processes");
            throw;
        }
    }

    /// <summary>
    /// Marks multiple processes as reviewed in bulk.
    /// </summary>
    public async Task<ProcessActionResponseData> BulkReviewedAsync(
        List<long> processIds,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bulk marking {Count} processes as reviewed", processIds.Count);

            var url = $"api/process/bulkReviewed?clientId={_config.ClientId}";
            if (!string.IsNullOrEmpty(reason))
            {
                url += $"&reason={Uri.EscapeDataString(reason)}";
            }

            var response = await _httpClient.PostAsJsonAsync(url, processIds, cancellationToken);
            await response.EnsureSuccessStatusCodeWithContentAsync();

            var result = await response.Content.ReadFromJsonAsync<ProcessActionResponseData>(
                cancellationToken: cancellationToken);

            return result ?? new ProcessActionResponseData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk marking processes as reviewed");
            throw;
        }
    }

    #endregion

    #region Process Manipulation

    /// <summary>
    /// Removes a step from a process.
    /// </summary>
    public async Task<Dictionary<string, object>> RemoveStepFromProcessAsync(
        long processId,
        long stepId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Removing step {StepId} from process {ProcessId}", stepId, processId);

            var url = $"api/process/removeStepIdFromProcess?processId={processId}&step={stepId}";
            var response = await _httpClient.PostAsync(url, null, cancellationToken);
            await response.EnsureSuccessStatusCodeWithContentAsync();

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
                cancellationToken: cancellationToken);

            return result ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing step {StepId} from process {ProcessId}", stepId, processId);
            throw;
        }
    }

    /// <summary>
    /// Resumes a process with a specific version.
    /// </summary>
    public async Task<Dictionary<string, object>> ResumeWithVersionAsync(
        long processId,
        int version,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Resuming process {ProcessId} with version {Version}", processId, version);

            var url = $"api/process/resumeWithVersion?processId={processId}&version={version}";
            var response = await _httpClient.PostAsync(url, null, cancellationToken);
            await response.EnsureSuccessStatusCodeWithContentAsync();

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
                cancellationToken: cancellationToken);

            return result ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming process {ProcessId} with version {Version}", processId, version);
            throw;
        }
    }

    #endregion

    #region API Mapping

    /// <summary>
    /// Invokes an API mapping with GET method.
    /// </summary>
    public async Task<JsonNode?> InvokeApiMappingGetAsync(
        string endpoint,
        string? requestId = null,
        string? correlationId = null,
        ApiCallType callType = ApiCallType.ASYNC,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Invoking API mapping GET: {Endpoint}", endpoint);

            var queryParams = new List<string> { $"apiCallType={callType}" };
            if (!string.IsNullOrEmpty(requestId))
                queryParams.Add($"id={Uri.EscapeDataString(requestId)}");
            if (!string.IsNullOrEmpty(correlationId))
                queryParams.Add($"correlationId={Uri.EscapeDataString(correlationId)}");

            var url = $"api/call/{endpoint}?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            await response.EnsureSuccessStatusCodeWithContentAsync();

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonNode.Parse(jsonString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking API mapping GET: {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>
    /// Invokes an API mapping with POST method.
    /// </summary>
    public async Task<JsonNode?> InvokeApiMappingPostAsync(
        string endpoint,
        Dictionary<string, object> input,
        string? requestId = null,
        string? correlationId = null,
        ApiCallType callType = ApiCallType.ASYNC,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Invoking API mapping POST: {Endpoint}", endpoint);

            var queryParams = new List<string> { $"apiCallType={callType}" };
            if (!string.IsNullOrEmpty(requestId))
                queryParams.Add($"id={Uri.EscapeDataString(requestId)}");
            if (!string.IsNullOrEmpty(correlationId))
                queryParams.Add($"correlationId={Uri.EscapeDataString(correlationId)}");

            var url = $"api/call/{endpoint}?{string.Join("&", queryParams)}";
            var response = await _httpClient.PostAsJsonAsync(url, input, cancellationToken);
            await response.EnsureSuccessStatusCodeWithContentAsync();

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonNode.Parse(jsonString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking API mapping POST: {Endpoint}", endpoint);
            throw;
        }
    }

    #endregion

    #region Process Definition Management

    /// <summary>
    /// Gets a process definition by namespace, name, and optional version.
    /// </summary>
    public async Task<ProcessDefinition> GetProcessDefinitionAsync(
        string @namespace,
        string name,
        int? version = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting process definition: {Namespace}:{Name} version {Version}", 
                @namespace, name, version);

            var url = $"api/processDefinitions/{Uri.EscapeDataString(@namespace)}/{Uri.EscapeDataString(name)}";
            if (version.HasValue)
            {
                url += $"?version={version.Value}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            await response.EnsureSuccessStatusCodeWithContentAsync();

            var processDefinition = await response.Content.ReadFromJsonAsync<ProcessDefinition>(
                cancellationToken: cancellationToken);

            if (processDefinition == null)
            {
                throw new InvalidOperationException("Failed to deserialize process definition");
            }

            return processDefinition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting process definition: {Namespace}:{Name}", @namespace, name);
            throw;
        }
    }

    /// <summary>
    /// Gets all process definitions.
    /// </summary>
    public async Task<List<ProcessDefinition>> GetAllProcessDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all process definitions");

            var response = await _httpClient.GetAsync("api/processDefinitions", cancellationToken);
            await response.EnsureSuccessStatusCodeWithContentAsync();

            var processDefinitions = await response.Content.ReadFromJsonAsync<List<ProcessDefinition>>(
                cancellationToken: cancellationToken);

            return processDefinitions ?? new List<ProcessDefinition>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all process definitions");
            throw;
        }
    }

    /// <summary>
    /// Creates a new process definition.
    /// </summary>
    public async Task<ProcessDefinition> CreateProcessDefinitionAsync(
        ProcessDefinition processDefinition,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating process definition: {Namespace}:{Name} v{Version}", 
                processDefinition.Namespace, processDefinition.Name, processDefinition.Version);

            var response = await _httpClient.PostAsJsonAsync(
                "api/processDefinitions",
                processDefinition,
                cancellationToken);

            await response.EnsureSuccessStatusCodeWithContentAsync();

            var created = await response.Content.ReadFromJsonAsync<ProcessDefinition>(
                cancellationToken: cancellationToken);

            if (created == null)
            {
                throw new InvalidOperationException("Failed to deserialize created process definition");
            }

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating process definition");
            throw;
        }
    }

    /// <summary>
    /// Updates an existing process definition.
    /// </summary>
    public async Task<ProcessDefinition> UpdateProcessDefinitionAsync(
        ProcessDefinition processDefinition,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating process definition: {Namespace}:{Name} v{Version}", 
                processDefinition.Namespace, processDefinition.Name, processDefinition.Version);

            var response = await _httpClient.PutAsJsonAsync(
                "api/processDefinitions",
                processDefinition,
                cancellationToken);

            await response.EnsureSuccessStatusCodeWithContentAsync();

            var updated = await response.Content.ReadFromJsonAsync<ProcessDefinition>(
                cancellationToken: cancellationToken);

            if (updated == null)
            {
                throw new InvalidOperationException("Failed to deserialize updated process definition");
            }

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating process definition");
            throw;
        }
    }

    /// <summary>
    /// Deletes process definitions.
    /// </summary>
    public async Task<object> DeleteProcessDefinitionsAsync(
        List<ProcessDefinition> processDefinitions,
        bool? versionOnly = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting {Count} process definitions", processDefinitions.Count);

            var url = "api/processDefinitions";
            if (versionOnly.HasValue)
            {
                url += $"?versionOnly={versionOnly.Value}";
            }

            var request = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = JsonContent.Create(processDefinitions)
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await response.EnsureSuccessStatusCodeWithContentAsync();

            var result = await response.Content.ReadFromJsonAsync<object>(
                cancellationToken: cancellationToken);

            return result ?? new { };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting process definitions");
            throw;
        }
    }

    #endregion
}
