using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unmeshed.Sdk.Models;

/// <summary>
/// Represents a work request from the Unmeshed engine.
/// </summary>
public class WorkRequest
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

    /// <summary>The name of the step.</summary>
    [JsonPropertyName("stepName")]
    public string StepName { get; set; } = string.Empty;

    /// <summary>The namespace of the step.</summary>
    [JsonPropertyName("stepNamespace")]
    public string StepNamespace { get; set; } = "default";

    /// <summary>The reference ID for the step.</summary>
    [JsonPropertyName("stepRef")]
    public string? StepRef { get; set; }

    /// <summary>The input data for the worker.</summary>
    [JsonPropertyName("inputParam")]
    [JsonConverter(typeof(Unmeshed.Sdk.Serialization.DictionaryStringObjectJsonConverter))]
    public Dictionary<string, object>? Input { get; set; }

    /// <summary>Whether the step is optional.</summary>
    [JsonPropertyName("isOptional")]
    public bool IsOptional { get; set; }

    /// <summary>Whether the step was polled.</summary>
    [JsonPropertyName("polled")]
    [JsonConverter(typeof(Unmeshed.Sdk.Serialization.BooleanOrNumberJsonConverter))]
    public bool Polled { get; set; }

    /// <summary>The timestamp when the step started.</summary>
    [JsonPropertyName("started")]
    public long Started { get; set; }

    /// <summary>The timestamp when the step was scheduled.</summary>
    [JsonPropertyName("scheduled")]
    public long Scheduled { get; set; }

    /// <summary>The timestamp when the step was last updated.</summary>
    [JsonPropertyName("updated")]
    public long Updated { get; set; }

    /// <summary>The priority of the step.</summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }
}
