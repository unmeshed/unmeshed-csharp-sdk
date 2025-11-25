using System.Text.Json.Serialization;

namespace Unmeshed.Sdk.Models;

/// <summary>
/// Represents a work response to be submitted to the Unmeshed engine.
/// </summary>
public class WorkResponse
{
    /// <summary>The ID of the step.</summary>
    [JsonPropertyName("stepId")]
    public long StepId { get; set; }

    /// <summary>The ID of the process instance.</summary>
    [JsonPropertyName("processId")]
    public long ProcessId { get; set; }

    /// <summary>The unique execution ID for this step attempt.</summary>
    [JsonPropertyName("stepExecutionId")]
    public long StepExecutionId { get; set; }

    /// <summary>The number of times this step has been run.</summary>
    [JsonPropertyName("runCount")]
    public int RunCount { get; set; }

    /// <summary>The output data from the worker.</summary>
    [JsonPropertyName("output")]
    public Dictionary<string, object> Output { get; set; } = new();

    /// <summary>The status of the execution (e.g., COMPLETED, FAILED, RUNNING).</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = UnmeshedConstants.StepStatus.Completed;

    /// <summary>Seconds to wait before rescheduling the worker.</summary>
    [JsonPropertyName("rescheduleAfterSeconds")]
    public int? RescheduleAfterSeconds { get; set; }

    /// <summary>The timestamp when execution started.</summary>
    [JsonPropertyName("startedAt")]
    public long StartedAt { get; set; }
}
