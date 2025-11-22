namespace Unmeshed.Sdk.Models;

/// <summary>
/// Represents the result of a worker execution.
/// </summary>
public class StepResult
{
    /// <summary>The output data from the worker.</summary>
    public Dictionary<string, object> Output { get; set; } = new();
    /// <summary>Whether the worker should keep running (deprecated, use RescheduleAfterSeconds).</summary>
    public bool? KeepRunning { get; set; }
    /// <summary>Seconds to wait before rescheduling the worker.</summary>
    public int? RescheduleAfterSeconds { get; set; }
    /// <summary>The status of the execution (e.g., COMPLETED, FAILED, RUNNING).</summary>
    public string? Status { get; set; }
    /// <summary>The timestamp when execution started.</summary>
    public long StartedAt { get; set; }
    /// <summary>The timestamp when execution completed.</summary>
    public long CompletedAt { get; set; }
}
