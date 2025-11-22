using System.Text.Json.Serialization;

namespace Unmeshed.Sdk.Models;

/// <summary>
/// Represents step size information for polling.
/// </summary>
public class StepSize
{
    /// <summary>The step queue identifier.</summary>
    [JsonPropertyName("stepQueueNameData")]
    public StepQueueNameData StepQueueNameData { get; set; } = new();

    /// <summary>The number of items in the queue.</summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }
}

/// <summary>
/// Represents the components of a step queue name.
/// </summary>
public class StepQueueNameData
{
    /// <summary>The process ID.</summary>
    [JsonPropertyName("processId")]
    public string ProcessId { get; set; } = string.Empty;

    /// <summary>The namespace.</summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = "default";

    /// <summary>The type of step.</summary>
    [JsonPropertyName("stepType")]
    public string StepType { get; set; } = string.Empty;

    /// <summary>The name of the step.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
