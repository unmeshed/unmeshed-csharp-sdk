namespace Unmeshed.Sdk.Workers;

/// <summary>
/// Attribute to mark a method as a worker function.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class WorkerFunctionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the worker.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the namespace of the worker.
    /// </summary>
    public string Namespace { get; set; } = "default";

    /// <summary>
    /// Gets or sets the workStepNames of the worker.
    /// </summary>
    public string[] WorkStepNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the maximum number of concurrent executions.
    /// </summary>
    public int MaxInProgress { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether this worker should run on IO thread pool.
    /// </summary>
    public bool IoThread { get; set; } = false;
}
