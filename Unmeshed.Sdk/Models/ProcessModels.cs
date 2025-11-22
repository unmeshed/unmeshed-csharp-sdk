using System.Text.Json.Serialization;

namespace Unmeshed.Sdk.Models;

/// <summary>
/// Represents process request data for running processes.
/// </summary>
public class ProcessRequestData
{
    /// <summary>The namespace of the process.</summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = "default";
    /// <summary>The name of the process.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    /// <summary>The input data for the process.</summary>
    [JsonPropertyName("input")]
    public Dictionary<string, object> Input { get; set; } = new();
    /// <summary>The correlation ID for tracking.</summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
    /// <summary>The request ID.</summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }
    /// <summary>The version of the process.</summary>
    [JsonPropertyName("version")]
    public int? Version { get; set; }
}

/// <summary>
/// Represents the data of a running process.
/// </summary>
public class ProcessData
{
    /// <summary>The unique ID of the process instance.</summary>
    [JsonPropertyName("processId")]
    public long ProcessId { get; set; }
    /// <summary>The namespace of the process.</summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;
    /// <summary>The name of the process.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    /// <summary>The current status of the process.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    /// <summary>The input data provided to the process.</summary>
    [JsonPropertyName("input")]
    public Dictionary<string, object> Input { get; set; } = new();
    /// <summary>The output data from the process.</summary>
    [JsonPropertyName("output")]
    public Dictionary<string, object> Output { get; set; } = new();
    /// <summary>The version of the process definition.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }
    /// <summary>The timestamp when the process was created.</summary>
    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; set; }
    /// <summary>The timestamp when the process was last updated.</summary>
    [JsonPropertyName("updatedAt")]
    public long UpdatedAt { get; set; }
    /// <summary>The timestamp when the process completed.</summary>
    [JsonPropertyName("completedAt")]
    public long? CompletedAt { get; set; }
}

/// <summary>
/// Represents search parameters for querying process executions.
/// </summary>
public class ProcessSearchRequest
{
    /// <summary>Start time epoch in milliseconds.</summary>
    [JsonPropertyName("startTimeEpoch")]
    public long? StartTimeEpoch { get; set; }
    
    /// <summary>End time epoch in milliseconds.</summary>
    [JsonPropertyName("endTimeEpoch")]
    public long? EndTimeEpoch { get; set; }
    
    /// <summary>Namespace to filter by.</summary>
    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }
    
    /// <summary>Process types to filter by.</summary>
    [JsonPropertyName("processTypes")]
    public List<string>? ProcessTypes { get; set; }
    
    /// <summary>Trigger types to filter by.</summary>
    [JsonPropertyName("triggerTypes")]
    public List<string>? TriggerTypes { get; set; }
    
    /// <summary>Process names to filter by.</summary>
    [JsonPropertyName("names")]
    public List<string>? Names { get; set; }
    
    /// <summary>Process IDs to filter by.</summary>
    [JsonPropertyName("processIds")]
    public List<long>? ProcessIds { get; set; }
    
    /// <summary>Correlation IDs to filter by.</summary>
    [JsonPropertyName("correlationIds")]
    public List<string>? CorrelationIds { get; set; }
    
    /// <summary>Request IDs to filter by.</summary>
    [JsonPropertyName("requestIds")]
    public List<string>? RequestIds { get; set; }
    
    /// <summary>Statuses to filter by.</summary>
    [JsonPropertyName("statuses")]
    public List<string>? Statuses { get; set; }
    
    /// <summary>Maximum number of results to return.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;
    
    /// <summary>Offset for pagination.</summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; } = 0;
}

/// <summary>
/// Represents the response from bulk process actions.
/// </summary>
public class ProcessActionResponseData
{
    /// <summary>Number of processes successfully affected.</summary>
    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }
    
    /// <summary>Number of processes that failed.</summary>
    [JsonPropertyName("failureCount")]
    public int FailureCount { get; set; }
    
    /// <summary>List of process IDs that were successful.</summary>
    [JsonPropertyName("successfulProcessIds")]
    public List<long> SuccessfulProcessIds { get; set; } = new();
    
    /// <summary>List of process IDs that failed.</summary>
    [JsonPropertyName("failedProcessIds")]
    public List<long> FailedProcessIds { get; set; } = new();
    
    /// <summary>Error messages if any.</summary>
    [JsonPropertyName("errors")]
    public Dictionary<string, string>? Errors { get; set; }
}

/// <summary>
/// Represents step data within a process.
/// </summary>
public class StepData
{
    /// <summary>The unique ID of the step.</summary>
    [JsonPropertyName("stepId")]
    public long StepId { get; set; }
    
    /// <summary>The process ID this step belongs to.</summary>
    [JsonPropertyName("processId")]
    public long ProcessId { get; set; }
    
    /// <summary>The namespace of the step.</summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;
    
    /// <summary>The name of the step.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>The type of the step.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>The current status of the step.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    /// <summary>The input data for the step.</summary>
    [JsonPropertyName("input")]
    public Dictionary<string, object> Input { get; set; } = new();
    
    /// <summary>The output data from the step.</summary>
    [JsonPropertyName("output")]
    public Dictionary<string, object> Output { get; set; } = new();
    
    /// <summary>The timestamp when the step was created.</summary>
    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; set; }
    
    /// <summary>The timestamp when the step was last updated.</summary>
    [JsonPropertyName("updatedAt")]
    public long UpdatedAt { get; set; }
    
    /// <summary>The timestamp when the step completed.</summary>
    [JsonPropertyName("completedAt")]
    public long? CompletedAt { get; set; }
}

/// <summary>
/// Represents a process definition.
/// </summary>
public class ProcessDefinition
{
    /// <summary>The namespace of the process definition.</summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = "default";
    
    /// <summary>The name of the process definition.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>The version of the process definition.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }
    
    /// <summary>The type of the process.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>Description of the process.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>The steps in the process definition.</summary>
    [JsonPropertyName("steps")]
    public List<StepDefinition> Steps { get; set; } = new();
    
    /// <summary>Additional metadata.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Represents a step definition within a process definition.
/// </summary>
public class StepDefinition
{
    /// <summary>The name of the step.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>The reference identifier for the step.</summary>
    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty;
    
    /// <summary>The type of the step.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>The namespace of the step.</summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = "default";
    
    /// <summary>The input configuration for the step.</summary>
    [JsonPropertyName("input")]
    public Dictionary<string, object> Input { get; set; } = new();
    
    /// <summary>Additional metadata for the step.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// API call type enumeration.
/// </summary>
public enum ApiCallType
{
    /// <summary>Synchronous API call.</summary>
    SYNC,
    /// <summary>Asynchronous API call.</summary>
    ASYNC
}
